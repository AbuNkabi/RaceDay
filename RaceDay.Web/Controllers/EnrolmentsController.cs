using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Participant")]
public class EnrolmentsController : BaseController
{
    public EnrolmentsController(ISessionService session, IRaceDayApiClient api) : base(session, api) { }

    public async Task<IActionResult> Index()
    {
        var enrolmentsResult = await Api.GetMyEnrolmentsAsync();

        var expired = RedirectIfSessionExpired(enrolmentsResult);
        if (expired != null) return expired;

        if (!enrolmentsResult.Success)
        {
            TempData["Error"] = enrolmentsResult.Error;
            return View(new List<MyEnrolmentViewModel>());
        }

        var eventsResult = await Api.GetEventsAsync();
        var events = (eventsResult.Data ?? new()).ToDictionary(e => e.EventId);

        var model = (enrolmentsResult.Data ?? new())
            .Select(e => new MyEnrolmentViewModel
            {
                Enrolment = e,
                Event = events.GetValueOrDefault(e.EventId)
            })
            .OrderBy(m => m.Event?.EventDate)
            .ToList();

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await Api.CancelEnrolmentAsync(id);

        var expired = RedirectIfSessionExpired(result);
        if (expired != null) return expired;

        if (result.Success)
            TempData["Success"] = "Your enrolment has been cancelled.";
        else
            TempData["Error"] = result.Error ?? "Could not cancel the enrolment.";

        return RedirectToAction(nameof(Index));
    }
}
