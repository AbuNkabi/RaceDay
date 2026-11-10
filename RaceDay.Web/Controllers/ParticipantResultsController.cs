using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[SessionAuthorize(Role = "Participant")]
public class ParticipantResultsController : BaseController
{
    public ParticipantResultsController(ISessionService session, IRaceDayApiClient api)
        : base(session, api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var myEnrolments = await Api.GetMyEnrolmentsAsync();
        var expired = RedirectIfSessionExpired(myEnrolments);
        if (expired != null) return expired;

        if (!myEnrolments.Success)
        {
            TempData["Error"] = myEnrolments.Error ?? "Could not load your enrolments.";
            return View(new List<MyRaceResultViewModel>());
        }

        var eventsResponse = await Api.GetEventsAsync();
        if (!eventsResponse.Success)
        {
            TempData["Error"] = eventsResponse.Error ?? "Could not load event information.";
            return View(new List<MyRaceResultViewModel>());
        }

        var events = (eventsResponse.Data ?? new List<EventModel>())
            .ToDictionary(e => e.EventId);
        var entries = myEnrolments.Data ?? new List<EnrolmentModel>();
        var myIds = entries.Select(e => e.EnrolmentId).ToHashSet();
        var output = new List<MyRaceResultViewModel>();

        foreach (var eventId in entries.Select(e => e.EventId).Distinct())
        {
            if (!events.TryGetValue(eventId, out var race)) continue;
            var results = await Api.GetEventResultsAsync(eventId);
            expired = RedirectIfSessionExpired(results);
            if (expired != null) return expired;

            if (!results.Success)
            {
                TempData["Error"] = "Some results could not be loaded. Please try again.";
                continue;
            }

            var categories = await Api.GetCategoriesAsync(eventId);
            var categoryNames = (categories.Data ?? new List<CategoryModel>())
                .ToDictionary(x => x.CategoryId, x => x.CategoryName);
            foreach (var result in results.Data ?? new List<EventResultModel>())
            {
                if (!myIds.Contains(result.EnrolmentId)) continue;
                output.Add(new MyRaceResultViewModel
                {
                    EventId = eventId,
                    EventName = race.EventName,
                    EventDate = race.EventDate,
                    CategoryName = result.CategoryId.HasValue
                        ? categoryNames.GetValueOrDefault(result.CategoryId.Value, "-") : "-",
                    Position = result.Position,
                    Time = result.Time
                });
            }
        }

        return View(output.OrderByDescending(r => r.EventDate).ToList());
    }
}
