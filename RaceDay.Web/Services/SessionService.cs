using System.Text.Json;

namespace RaceDay.Web.Services;

public class SessionUser
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public interface ISessionService
{
    SessionUser? CurrentUser { get; }
    bool IsAuthenticated { get; }
    bool IsOrganiser { get; }
    bool IsParticipant { get; }
    void SignIn(SessionUser user);
    void SignOut();
}

public class SessionService : ISessionService
{
    private const string Key = "RaceDay.User";
    private readonly IHttpContextAccessor _accessor;

    public SessionService(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    public SessionUser? CurrentUser
    {
        get
        {
            var json = Session.GetString(Key);
            return string.IsNullOrEmpty(json)
                ? null
                : JsonSerializer.Deserialize<SessionUser>(json);
        }
    }

    public bool IsAuthenticated => CurrentUser != null;
    public bool IsOrganiser => CurrentUser?.Role == "Organiser";
    public bool IsParticipant => CurrentUser?.Role == "Participant";

    public void SignIn(SessionUser user) =>
        Session.SetString(Key, JsonSerializer.Serialize(user));

    public void SignOut() => Session.Clear();
}
