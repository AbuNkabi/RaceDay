using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Contracts;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public bool Unauthorized => StatusCode == HttpStatusCode.Unauthorized;
}

public interface IRaceDayApiClient
{
    // Auth + profile
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResult<object>> RegisterAsync(RegisterRequest request);
    Task<ApiResult<ProfileModel>> GetProfileAsync();
    Task<ApiResult<object>> UpdateProfileAsync(UpdateProfileRequest request);

    // Events
    Task<ApiResult<List<EventModel>>> GetEventsAsync();
    Task<ApiResult<List<EventModel>>> GetMyEventsAsync();
    Task<ApiResult<EventModel>> GetEventAsync(int id);
    Task<ApiResult<EventModel>> CreateEventAsync(EventRequest request);
    Task<ApiResult<object>> UpdateEventAsync(int id, EventRequest request);
    Task<ApiResult<object>> DeleteEventAsync(int id);
    Task<ApiResult<EventModel>> UploadEventImageAsync(int id, IFormFile file);
    Task<ApiResult<List<EventEnrolmentModel>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult<List<EventResultModel>>> GetEventResultsAsync(int eventId);

    Task<ApiResult<List<CategoryModel>>> GetCategoriesAsync(int eventId);
    Task<ApiResult<object>> CreateCategoryAsync(int eventId, CategoryFormViewModel model);
    Task<ApiResult<object>> UpdateCategoryAsync(int eventId, int categoryId, CategoryFormViewModel model);
    Task<ApiResult<object>> DeleteCategoryAsync(int eventId, int categoryId);
    Task<ApiResult<object>> ChangeEnrolmentStatusAsync(int eventId, int enrolmentId, string status);
    Task<ApiResult<object>> UploadProfileImageAsync(IFormFile file);
    Task<(byte[] Bytes, string ContentType)?> GetEventImageAsync(int eventId);
    Task<(byte[] Bytes, string ContentType)?> GetProfileImageAsync();

    // Enrolments
    Task<ApiResult<List<EnrolmentModel>>> GetMyEnrolmentsAsync();
    Task<ApiResult<EnrolmentModel>> EnrolAsync(int eventId, int? categoryId);
    Task<ApiResult<object>> CancelEnrolmentAsync(int enrolmentId);

    // Results
    Task<ApiResult<object>> CreateResultAsync(ResultBody body);
    Task<ApiResult<object>> DeleteResultAsync(int resultId);
}

public class RaceDayApiClient : IRaceDayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ISessionService _session;

    public RaceDayApiClient(HttpClient http, ISessionService session)
    {
        _http = http;
        _session = session;
    }

    // ---- helpers ----
    private HttpRequestMessage Build(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        var token = _session.CurrentUser?.Token;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return request;
    }

    private static HttpContent Json<T>(T body) => JsonContent.Create(body, options: JsonOptions);

    private async Task<ApiResult<T>> SendAsync<T>(HttpRequestMessage request)
    {
        using var ownedRequest = request;
        try
        {
            using var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                T? data = default;
                if (response.StatusCode != HttpStatusCode.NoContent &&
                    response.Content.Headers.ContentLength != 0)
                {
                    try
                    {
                        data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
                    }
                    catch (JsonException)
                    {
                        // Body wasn't JSON (e.g. plain text); the call still succeeded.
                    }
                }
                return new ApiResult<T> { Success = true, Data = data, StatusCode = response.StatusCode };
            }

            var body = await response.Content.ReadAsStringAsync();
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = response.StatusCode,
                Error = ExtractError(response.StatusCode, body)
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Error = "The RaceDay API is unreachable. Please try again later."
            };
        }
        catch (TaskCanceledException)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = HttpStatusCode.GatewayTimeout,
                Error = "The RaceDay API took too long to respond."
            };
        }
    }

    private static string ExtractError(HttpStatusCode status, string body)
    {
        if (status == HttpStatusCode.Forbidden) return "You do not have permission to do that.";
        if (status == HttpStatusCode.Unauthorized && string.IsNullOrWhiteSpace(body))
            return "Your session has expired. Please log in again.";
        if (string.IsNullOrWhiteSpace(body)) return $"Request failed ({(int)status}).";

        // Plain text (e.g. BadRequest("message")) or ProblemDetails JSON
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.String)
                return doc.RootElement.GetString() ?? "Request failed.";

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(p => p.Value.EnumerateArray().Select(v => v.GetString()))
                        .Where(m => !string.IsNullOrEmpty(m));
                    return string.Join(" ", messages);
                }
                if (doc.RootElement.TryGetProperty("title", out var title))
                    return title.GetString() ?? "Request failed.";
            }
        }
        catch (JsonException)
        {
            // not JSON - fall through
        }

        return body.Length > 200 ? body[..200] : body;
    }

    // ---- Auth + profile ----
    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request) =>
        SendAsync<AuthResponse>(Build(HttpMethod.Post, "api/auth/login", Json(request)));

    public Task<ApiResult<object>> RegisterAsync(RegisterRequest request) =>
        SendAsync<object>(Build(HttpMethod.Post, "api/auth/register", Json(request)));

    public Task<ApiResult<ProfileModel>> GetProfileAsync() =>
        SendAsync<ProfileModel>(Build(HttpMethod.Get, "api/profile/me"));

    public Task<ApiResult<object>> UpdateProfileAsync(UpdateProfileRequest request) =>
        SendAsync<object>(Build(HttpMethod.Put, "api/profile/me", Json(request)));

    // ---- Events ----
    public Task<ApiResult<List<EventModel>>> GetEventsAsync() =>
        SendAsync<List<EventModel>>(Build(HttpMethod.Get, "api/events"));

    public Task<ApiResult<List<EventModel>>> GetMyEventsAsync() =>
        SendAsync<List<EventModel>>(Build(HttpMethod.Get, "api/events/mine"));

    public Task<ApiResult<EventModel>> GetEventAsync(int id) =>
        SendAsync<EventModel>(Build(HttpMethod.Get, $"api/events/{id}"));

    public Task<ApiResult<EventModel>> CreateEventAsync(EventRequest request) =>
        SendAsync<EventModel>(Build(HttpMethod.Post, "api/events", Json(request)));

    public Task<ApiResult<object>> UpdateEventAsync(int id, EventRequest request) =>
        SendAsync<object>(Build(HttpMethod.Put, $"api/events/{id}", Json(request)));

    public Task<ApiResult<object>> DeleteEventAsync(int id) =>
        SendAsync<object>(Build(HttpMethod.Delete, $"api/events/{id}"));

    public async Task<ApiResult<EventModel>> UploadEventImageAsync(int id, IFormFile file)
    {
        using var form = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);

        return await SendAsync<EventModel>(Build(HttpMethod.Post, $"api/events/{id}/image", form));
    }

    public Task<ApiResult<List<EventEnrolmentModel>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EventEnrolmentModel>>(Build(HttpMethod.Get, $"api/events/{eventId}/enrolments"));

    public Task<ApiResult<List<EventResultModel>>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<EventResultModel>>(Build(HttpMethod.Get, $"api/events/{eventId}/results"));

    public Task<ApiResult<List<CategoryModel>>> GetCategoriesAsync(int eventId) =>
        SendAsync<List<CategoryModel>>(Build(HttpMethod.Get, $"api/events/{eventId}/categories"));

    public Task<ApiResult<object>> CreateCategoryAsync(int eventId, CategoryFormViewModel model) =>
        SendAsync<object>(Build(HttpMethod.Post, $"api/events/{eventId}/categories",
            Json(new { model.CategoryName, model.Description, model.EntryFee, model.MaximumParticipants })));

    public Task<ApiResult<object>> UpdateCategoryAsync(int eventId, int categoryId, CategoryFormViewModel model) =>
        SendAsync<object>(Build(HttpMethod.Put, $"api/events/{eventId}/categories/{categoryId}",
            Json(new { model.CategoryName, model.Description, model.EntryFee, model.MaximumParticipants })));

    public Task<ApiResult<object>> DeleteCategoryAsync(int eventId, int categoryId) =>
        SendAsync<object>(Build(HttpMethod.Delete, $"api/events/{eventId}/categories/{categoryId}"));

    public Task<ApiResult<object>> ChangeEnrolmentStatusAsync(int eventId, int enrolmentId, string status) =>
        SendAsync<object>(Build(HttpMethod.Put, $"api/events/{eventId}/enrolments/{enrolmentId}/status", Json(new { status })));

    public async Task<ApiResult<object>> UploadProfileImageAsync(IFormFile file)
    {
        using var form = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        using var content = new StreamContent(stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(content, "file", file.FileName);
        return await SendAsync<object>(Build(HttpMethod.Post, "api/profile/me/image", form));
    }

    private async Task<(byte[] Bytes, string ContentType)?> FetchImageAsync(string url)
    {
        using var req = Build(HttpMethod.Get, url);
        using var res = await _http.SendAsync(req);
        if (!res.IsSuccessStatusCode) return null;
        return (await res.Content.ReadAsByteArrayAsync(), res.Content.Headers.ContentType?.MediaType ?? "image/jpeg");
    }
    public Task<(byte[] Bytes, string ContentType)?> GetEventImageAsync(int eventId) =>
        FetchImageAsync($"api/events/{eventId}/image");
    public Task<(byte[] Bytes, string ContentType)?> GetProfileImageAsync() =>
        FetchImageAsync("api/profile/me/image");

    // ---- Enrolments ----
    public Task<ApiResult<List<EnrolmentModel>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<EnrolmentModel>>(Build(HttpMethod.Get, "api/enrolments"));

    public Task<ApiResult<EnrolmentModel>> EnrolAsync(int eventId, int? categoryId) =>
        SendAsync<EnrolmentModel>(Build(HttpMethod.Post, "api/enrolments",
            Json(new CreateEnrolmentBody { EventId = eventId, CategoryId = categoryId })));

    public Task<ApiResult<object>> CancelEnrolmentAsync(int enrolmentId) =>
        SendAsync<object>(Build(HttpMethod.Delete, $"api/enrolments/{enrolmentId}"));

    // ---- Results ----
    public Task<ApiResult<object>> CreateResultAsync(ResultBody body) =>
        SendAsync<object>(Build(HttpMethod.Post, "api/results", Json(body)));

    public Task<ApiResult<object>> DeleteResultAsync(int resultId) =>
        SendAsync<object>(Build(HttpMethod.Delete, $"api/results/{resultId}"));
}
