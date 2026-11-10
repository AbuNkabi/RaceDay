using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : BaseController
{
    public HomeController(ISessionService session, IRaceDayApiClient api) : base(session, api) { }

    public async Task<IActionResult> Index()
    {
        // Show a few upcoming events on the landing page
        var result = await Api.GetEventsAsync();
        var upcoming = (result.Data ?? new List<EventModel>())
            .Where(e => e.EventDate >= DateTime.Today)
            .OrderBy(e => e.EventDate)
            .Take(3)
            .ToList();

        if (!result.Success) ViewBag.ApiError = result.Error;

        return View(upcoming);
    }

    public IActionResult Error(string? message = null) =>
        View(new ErrorViewModel { Message = message });
}
