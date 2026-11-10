using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Session-based authorisation. Redirects anonymous users to Login and
/// users with the wrong role to the Access Denied page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public string? Role { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.RequestServices.GetRequiredService<ISessionService>();
        var user = session.CurrentUser;

        if (user == null)
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult(
                "Login", "Account", new { returnUrl });
            return;
        }

        if (!string.IsNullOrEmpty(Role) && user.Role != Role)
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
