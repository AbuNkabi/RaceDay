using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Organisers record results for their events
[SessionAuthorize(Role = "Organiser")]
public class ResultsController : BaseController
{
    public ResultsController(ISessionService session, IRaceDayApiClient api) : base(session, api) { }

    [HttpGet]
    public async Task<IActionResult> Manage(int eventId)
    {
        var model = await LoadAsync(eventId);
        if (model == null) return RedirectToAction("Mine", "Events");

        model.Position = model.Results.Count + 1;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Manage(int eventId, ManageResultsViewModel form)
    {
        if (!ModelState.IsValid)
        {
            var reload = await LoadAsync(eventId);
            if (reload == null) return RedirectToAction("Mine", "Events");
            reload.EnrolmentId = form.EnrolmentId;
            reload.Position = form.Position;
            reload.Time = form.Time;
            return View(reload);
        }

        // Never trust the posted eventId or enrolmentId. Verify this organiser owns
        // the event and the enrolment belongs to that event before calling the API.
        var allowed = await LoadAsync(eventId);
        if (allowed == null) return RedirectToAction("Mine", "Events");
        if (!allowed.Enrolments.Any(e => e.EnrolmentId == form.EnrolmentId))
        {
            TempData["Error"] = "That participant is not available for result entry in this event.";
            return RedirectToAction(nameof(Manage), new { eventId });
        }

        var result = await Api.CreateResultAsync(new ResultBody
        {
            EnrolmentId = form.EnrolmentId,
            Position = form.Position,
            Time = form.Time
        });

        var expired = RedirectIfSessionExpired(result);
        if (expired != null) return expired;

        if (result.Success)
            TempData["Success"] = "Result recorded.";
        else
            TempData["Error"] = result.Error ?? "Could not record the result.";

        return RedirectToAction(nameof(Manage), new { eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int resultId)
    {
        var allowed = await LoadAsync(eventId);
        if (allowed == null) return RedirectToAction("Mine", "Events");
        if (!allowed.Results.Any(r => r.ResultId == resultId))
        {
            TempData["Error"] = "That result is not part of your event.";
            return RedirectToAction(nameof(Manage), new { eventId });
        }

        var result = await Api.DeleteResultAsync(resultId);

        var expired = RedirectIfSessionExpired(result);
        if (expired != null) return expired;

        if (result.Success)
            TempData["Success"] = "Result removed.";
        else
            TempData["Error"] = result.Error ?? "Could not remove the result.";

        return RedirectToAction(nameof(Manage), new { eventId });
    }

    private async Task<ManageResultsViewModel?> LoadAsync(int eventId)
    {
        var ev = await Api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data == null)
        {
            TempData["Error"] = ev.Error ?? "Event not found.";
            return null;
        }

        if (ev.Data.OrganiserId != Session.CurrentUser!.UserId)
        {
            TempData["Error"] = "You can only manage results for your own events.";
            return null;
        }

        var enrolments = await Api.GetEventEnrolmentsAsync(eventId);
        var results = await Api.GetEventResultsAsync(eventId);

        var expired = RedirectIfSessionExpired(enrolments);
        if (expired != null) return null;
        expired = RedirectIfSessionExpired(results);
        if (expired != null) return null;
        if (!enrolments.Success || !results.Success)
        {
            TempData["Error"] = enrolments.Error ?? results.Error ?? "Could not load results.";
            return null;
        }

        var resultList = (results.Data ?? new()).OrderBy(r => r.Position).ToList();
        var recorded = resultList.Select(r => r.EnrolmentId).ToHashSet();

        return new ManageResultsViewModel
        {
            Event = ev.Data,
            // only offer participants who don't have a result yet
            Enrolments = (enrolments.Data ?? new()).Where(e => e.Status == "Confirmed" && !recorded.Contains(e.EnrolmentId)).ToList(),
            Results = resultList
        };
    }
}
