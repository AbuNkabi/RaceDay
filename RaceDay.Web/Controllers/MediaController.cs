using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;
public class MediaController : Controller
{
    private readonly IRaceDayApiClient _api;
    private readonly ISessionService _session;
    public MediaController(IRaceDayApiClient api, ISessionService session)
    { _api = api; _session = session; }

    [HttpGet]
    public async Task<IActionResult> Event(int id)
    {
        var blob = await _api.GetEventImageAsync(id);
        return blob == null ? NotFound() : File(blob.Value.Bytes, blob.Value.ContentType);
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        if (!_session.IsAuthenticated) return Unauthorized();
        var blob = await _api.GetProfileImageAsync();
        return blob == null ? NotFound() : File(blob.Value.Bytes, blob.Value.ContentType);
    }
}
