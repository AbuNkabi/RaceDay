
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : BaseController
{
    public AccountController(
        ISessionService session,
        IRaceDayApiClient api) : base(session, api)
    {
    }

    // Login
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (Session.IsAuthenticated)
            return RedirectToAction("Index", "Events");

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var result = await Api.LoginAsync(new LoginRequest
        {
            Email = model.Email.Trim().ToLowerInvariant(),
            Password = model.Password
        });

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Error ?? "Login failed.");

            return View(model);
        }

        var auth = result.Data;

        // Store JWT and user details in the MVC session.
        Session.SignIn(new SessionUser
        {
            UserId = auth.UserId,
            Email = auth.Email,
            Role = auth.Role,
            Token = auth.Token,
            FullName = auth.Email
        });

        // Load the user's full profile name.
        var profile = await Api.GetProfileAsync();

        if (profile.Success && profile.Data != null)
        {
            Session.SignIn(new SessionUser
            {
                UserId = auth.UserId,
                Email = auth.Email,
                Role = auth.Role,
                Token = auth.Token,
                FullName = profile.Data.FullName
            });
        }

        if (!string.IsNullOrEmpty(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return auth.Role == "Organiser"
            ? RedirectToAction("Index", "Organiser")
            : RedirectToAction("Index", "Events");
    }

    // Register
    [HttpGet]
    public IActionResult Register()
    {
        if (Session.IsAuthenticated)
            return RedirectToAction("Index", "Events");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await Api.RegisterAsync(new RegisterRequest
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email.Trim().ToLowerInvariant(),
            Password = model.Password,
            Role = model.Role,
            OrganiserCode = model.OrganiserCode
        });

        if (!result.Success)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Error ?? "Registration failed.");

            return View(model);
        }

        TempData["Success"] =
            "Account created! You can now log in.";

        return RedirectToAction(nameof(Login));
    }

    // Logout
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Session.SignOut();

        return RedirectToAction("Index", "Home");
    }

    // View profile
    [SessionAuthorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var result = await Api.GetProfileAsync();

        var expired = RedirectIfSessionExpired(result);

        if (expired != null)
            return expired;

        if (!result.Success || result.Data == null)
        {
            TempData["Error"] =
                result.Error ?? "Could not load your profile.";

            return RedirectToAction("Index", "Events");
        }

        return View(new ProfileViewModel
        {
            FullName = result.Data.FullName,
            Email = result.Data.Email,
            Role = result.Data.Role,
            ProfileImageUrl = result.Data.ProfileImageUrl
        });
    }

    // Update profile
    [SessionAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(
        ProfileViewModel model)
    {
        var user = Session.CurrentUser!;

        model.Role = user.Role;

        if (!ModelState.IsValid)
            return View(model);

        var email = model.Email.Trim().ToLowerInvariant();

        var result = await Api.UpdateProfileAsync(
            new UpdateProfileRequest
            {
                FullName = model.FullName.Trim(),
                Email = email
            });

        var expired = RedirectIfSessionExpired(result);

        if (expired != null)
            return expired;

        if (!result.Success)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Error ?? "Could not update profile.");

            return View(model);
        }

        user.FullName = model.FullName.Trim();
        user.Email = email;

        Session.SignIn(user);

        TempData["Success"] = "Profile updated.";

        return RedirectToAction(nameof(Profile));
    }

    // Upload profile image through the API
    [SessionAuthorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(
        IFormFile? image)
    {
        if (image == null ||
            image.Length == 0 ||
            image.Length > 5 * 1024 * 1024 ||
            !new[] { "image/png", "image/jpeg", "image/webp" }
                .Contains(image.ContentType) ||
            !new[] { ".jpg", ".jpeg", ".png", ".webp" }
                .Contains(
                    Path.GetExtension(image.FileName)
                        .ToLowerInvariant()))
        {
            TempData["Error"] =
                "Choose a JPG, PNG or WEBP image smaller than 5 MB.";

            return RedirectToAction(nameof(Profile));
        }

        var uploaded = await Api.UploadProfileImageAsync(image);

        var expired = RedirectIfSessionExpired(uploaded);

        if (expired != null)
            return expired;

        TempData[uploaded.Success ? "Success" : "Error"] =
            uploaded.Success
                ? "Profile image saved."
                : uploaded.Error ?? "Upload failed.";

        return RedirectToAction(nameof(Profile));
    }

    // Access denied
    public IActionResult AccessDenied()
    {
        return View();
    }
}
