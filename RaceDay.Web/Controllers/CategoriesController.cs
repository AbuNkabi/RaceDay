using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;
[SessionAuthorize(Role = "Organiser")]
public class CategoriesController : BaseController
{
    public CategoriesController(ISessionService session, IRaceDayApiClient api) : base(session, api) { }
    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await Api.GetEventAsync(eventId);
        if (!ev.Success || ev.Data == null || ev.Data.OrganiserId != Session.CurrentUser!.UserId)
            return RedirectToAction("AccessDenied", "Account");
        var categories = await Api.GetCategoriesAsync(eventId);
        if (!categories.Success) TempData["Error"] = categories.Error ?? "Could not load categories.";
        ViewBag.EventName = ev.Data.EventName;
        return View(new CategoryFormViewModel { EventId = eventId, Categories = categories.Data ?? new() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel input)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index), new { eventId = input.EventId });
        var result = await Api.CreateCategoryAsync(input.EventId, input);
        var expired = RedirectIfSessionExpired(result);
        if (expired != null) return expired;
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Category added." : (result.Error ?? "Category failed.");
        return RedirectToAction(nameof(Index), new { eventId = input.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int eventId, int categoryId, CategoryFormViewModel input)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Index), new { eventId });
        var result = await Api.UpdateCategoryAsync(eventId, categoryId, input);
        var expired = RedirectIfSessionExpired(result); if (expired != null) return expired;
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Category updated." : (result.Error ?? "Update failed.");
        return RedirectToAction(nameof(Index), new { eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int eventId, int categoryId)
    {
        var result = await Api.DeleteCategoryAsync(eventId, categoryId);
        var expired = RedirectIfSessionExpired(result); if (expired != null) return expired;
        TempData[result.Success ? "Success" : "Error"] = result.Success ? "Category deleted." : (result.Error ?? "Delete failed.");
        return RedirectToAction(nameof(Index), new { eventId });
    }
}
