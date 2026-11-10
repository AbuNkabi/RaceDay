using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public abstract class BaseController : Controller
{
    protected readonly ISessionService Session;
    protected readonly IRaceDayApiClient Api;

    protected BaseController(ISessionService session, IRaceDayApiClient api)
    {
        Session = session;
        Api = api;
    }

    /// <summary>
    /// If the API says our token is no longer valid, clear the session and send the user to Login.
    /// </summary>
    protected IActionResult? RedirectIfSessionExpired<T>(ApiResult<T> result)
    {
        if (!result.Unauthorized) return null;

        Session.SignOut();
        TempData["Error"] = "Your session has expired. Please log in again.";
        return RedirectToAction("Login", "Account");
    }
}
