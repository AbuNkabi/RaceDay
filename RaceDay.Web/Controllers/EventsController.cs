using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class EventsController : BaseController
{
    private static readonly string[] AllowedImageTypes =
        { "image/jpeg", "image/png", "image/gif", "image/webp" };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public EventsController(ISessionService session, IRaceDayApiClient api) : base(session, api) { }

    // ---------- Public browsing (everyone) ----------
    public async Task<IActionResult> Index(string? search)
    {
        var result = await Api.GetEventsAsync();
        var events = result.Data ?? new List<EventModel>();

        if (!result.Success) TempData["Error"] = result.Error;

        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events.Where(e =>
                e.EventName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Location.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.Search = search;
        return View(events.Where(e => e.EventDate >= DateTime.Today).OrderBy(e => e.EventDate).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var eventResult = await Api.GetEventAsync(id);
        if (!eventResult.Success || eventResult.Data == null)
        {
            TempData["Error"] = eventResult.StatusCode == System.Net.HttpStatusCode.NotFound
                ? "That event could not be found."
                : eventResult.Error;
            return RedirectToAction(nameof(Index));
        }

        var model = new EventDetailsViewModel { Event = eventResult.Data };
        var cats = await Api.GetCategoriesAsync(id);
        if (cats.Success) model.Categories = cats.Data ?? new();

        if (Session.IsAuthenticated)
        {
            var resultsResult = await Api.GetEventResultsAsync(id);
            if (resultsResult.Success)
                model.Results = (resultsResult.Data ?? new()).OrderBy(r => r.Position).ToList();

            if (Session.IsParticipant)
            {
                var enrolments = await Api.GetMyEnrolmentsAsync();
                var mine = enrolments.Data?.FirstOrDefault(e => e.EventId == id);
                model.IsEnrolled = mine != null;
                model.EnrolmentId = mine?.EnrolmentId;
            }
        }

        return View(model);
    }

    // ---------- Participant: enrol ----------
    [SessionAuthorize(Role = "Participant")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int id, int? categoryId)
    {
        var result = await Api.EnrolAsync(id, categoryId);

        var expired = RedirectIfSessionExpired(result);
        if (expired != null) return expired;

        if (result.Success)
            TempData["Success"] = "You are enrolled in this event!";
        else
            TempData["Error"] = result.Error ?? "Could not enrol.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // ---------- Organiser: my events ----------
    [SessionAuthorize(Role = "Organiser")]
    public async Task<IActionResult> Mine()
    {
        var result = await Api.GetMyEventsAsync();

        var expired = RedirectIfSessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success) TempData["Error"] = result.Error;

        return View((result.Data ?? new()).OrderBy(e => e.EventDate).ToList());
    }

    [SessionAuthorize(Role = "Organiser")]
    [HttpGet]
    public IActionResult Create() => View(new EventFormViewModel());

    [SessionAuthorize(Role = "Organiser")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        ValidateImage(model.Image);
        if (!ModelState.IsValid) return View(model);

        var created = await Api.CreateEventAsync(ToRequest(model));

        var expired = RedirectIfSessionExpired(created);
        if (expired != null) return expired;

        if (!created.Success || created.Data == null)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "Could not create the event.");
            return View(model);
        }

        if (model.Image is { Length: > 0 })
        {
            var upload = await Api.UploadEventImageAsync(created.Data.EventId, model.Image);
            if (!upload.Success)
            {
                TempData["Error"] = $"Event created, but the image upload failed: {upload.Error}";
                return RedirectToAction(nameof(Mine));
            }
        }

        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Mine));
    }

    [SessionAuthorize(Role = "Organiser")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await Api.GetEventAsync(id);
        if (!result.Success || result.Data == null)
        {
            TempData["Error"] = result.Error ?? "Event not found.";
            return RedirectToAction(nameof(Mine));
        }

        if (result.Data.OrganiserId != Session.CurrentUser!.UserId)
            return RedirectToAction("AccessDenied", "Account");

        var e = result.Data;
        return View(new EventFormViewModel
        {
            EventId = e.EventId,
            EventName = e.EventName,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            ExistingImageUrl = e.ImageUrl
        });
    }

    [SessionAuthorize(Role = "Organiser")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        ValidateImage(model.Image);
        if (!ModelState.IsValid) return View(model);

        var updated = await Api.UpdateEventAsync(id, ToRequest(model));

        var expired = RedirectIfSessionExpired(updated);
        if (expired != null) return expired;

        if (!updated.Success)
        {
            ModelState.AddModelError(string.Empty, updated.Error ?? "Could not update the event.");
            return View(model);
        }

        if (model.Image is { Length: > 0 })
        {
            var upload = await Api.UploadEventImageAsync(id, model.Image);
            if (!upload.Success)
            {
                TempData["Error"] = $"Event updated, but the image upload failed: {upload.Error}";
                return RedirectToAction(nameof(Mine));
            }
        }

        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Mine));
    }

    [SessionAuthorize(Role = "Organiser")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await Api.DeleteEventAsync(id);

        var expired = RedirectIfSessionExpired(result);
        if (expired != null) return expired;

        if (result.Success)
            TempData["Success"] = "Event deleted.";
        else
            TempData["Error"] = result.Error ?? "Could not delete the event.";

        return RedirectToAction(nameof(Mine));
    }

    // ---------- helpers ----------
    private static EventRequest ToRequest(EventFormViewModel m) => new()
    {
        EventName = m.EventName.Trim(),
        Description = m.Description.Trim(),
        EventDate = m.EventDate,
        Location = m.Location.Trim()
    };

    private void ValidateImage(IFormFile? file)
    {
        if (file == null || file.Length == 0) return;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
            ModelState.AddModelError(nameof(EventFormViewModel.Image),
                "Only JPG, PNG, GIF or WebP file extensions are allowed.");

        if (!AllowedImageTypes.Contains(file.ContentType))
            ModelState.AddModelError(nameof(EventFormViewModel.Image),
                "Only JPG, PNG, GIF or WebP images are allowed.");

        if (file.Length > MaxImageBytes)
            ModelState.AddModelError(nameof(EventFormViewModel.Image),
                "The image must be 5 MB or smaller.");
    }
}
