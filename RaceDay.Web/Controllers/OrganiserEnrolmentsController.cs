using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;
[SessionAuthorize(Role = "Organiser")]
public class OrganiserEnrolmentsController : BaseController
{
    public OrganiserEnrolmentsController(ISessionService s, IRaceDayApiClient a) : base(s, a) { }
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await Api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data == null || ev.Data.OrganiserId != Session.CurrentUser!.UserId)
            return RedirectToAction("AccessDenied", "Account");
        var data = await Api.GetEventEnrolmentsAsync(eventId);
        var expired = RedirectIfSessionExpired(data);
        if (expired != null) return expired;
        if (!data.Success) TempData["Error"] = data.Error ?? "Failed to load enrolments.";
        ViewBag.EventId = eventId; ViewBag.EventName = ev.Data.EventName;
        return View(data.Data ?? new List<EventEnrolmentModel>());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int eventId, int enrolmentId, string status)
    {
        var result = await Api.ChangeEnrolmentStatusAsync(eventId, enrolmentId, status);
        var expired = RedirectIfSessionExpired(result);
        if (expired != null) return expired;
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Enrolment updated." : (result.Error ?? "Update failed.");
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
