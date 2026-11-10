using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;
[SessionAuthorize(Role = "Organiser")]
public class OrganiserController : BaseController
{
    public OrganiserController(ISessionService s, IRaceDayApiClient api) : base(s, api) { }
    public async Task<IActionResult> Index()
    {
        var races = await Api.GetMyEventsAsync();
        var expired = RedirectIfSessionExpired(races); if (expired != null) return expired;
        if (!races.Success) TempData["Error"] = races.Error ?? "Could not load dashboard.";
        var model = new OrganiserDashboardViewModel { Events = races.Data ?? new() };
        model.EventCount = model.Events.Count;
        foreach (var ev in model.Events)
        {
            var en = await Api.GetEventEnrolmentsAsync(ev.EventId);
            var results = await Api.GetEventResultsAsync(ev.EventId);
            if (!en.Success || !results.Success) { TempData["Error"] = "Some dashboard totals are unavailable."; continue; }
            model.EnrolmentCount += en.Data?.Count ?? 0;
            model.ConfirmedCount += en.Data?.Count(x => x.Status == "Confirmed") ?? 0;
            model.ResultCount += results.Data?.Count ?? 0;
        }
        return View(model);
    }
}
