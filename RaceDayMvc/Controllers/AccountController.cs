using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;
using RaceDayMvc.Models;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Controllers;

public class AccountController : AppController
{
    private readonly IRaceDayApi _api;

    public AccountController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (CurrentUserId is not null) return RedirectAfterLogin(returnUrl);

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _api.LoginAsync(new LoginRequest(model.Email.Trim(), model.Password));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty,
                result.IsUnauthorized ? "Incorrect email or password." : result.Error ?? "We couldn't log you in.");
            return View(model);
        }

        StartSession(result.Data!);
        Success($"Welcome back, {result.Data!.FirstName}!");
        return RedirectAfterLogin(model.ReturnUrl);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (CurrentUserId is not null) return RedirectAfterLogin(null);

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var phone = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        var registered = await _api.RegisterAsync(new RegisterRequest(
            model.FirstName.Trim(), model.LastName.Trim(), model.Email.Trim(), model.Password, model.Role, phone));

        if (!registered.IsSuccess)
        {
            if (registered.IsConflict)
                ModelState.AddModelError(nameof(model.Email), "That email address is already registered. Try logging in instead.");
            else
                ModelState.AddModelError(string.Empty, registered.Error ?? "We couldn't create your account.");

            return View(model);
        }

        // Log them straight in so they don't have to type their details twice
        var login = await _api.LoginAsync(new LoginRequest(model.Email.Trim(), model.Password));
        if (!login.IsSuccess)
        {
            Success("Your account is ready. Please log in.");
            return RedirectToAction(nameof(Login));
        }

        StartSession(login.Data!);
        Success($"Welcome to RaceDay, {login.Data!.FirstName}!");
        return RedirectAfterLogin(null);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        // End the API session first (best effort), then forget everything on our side
        await _api.LogoutAsync();
        HttpContext.Session.Clear();

        Success("You've been logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectAfterLogin(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        // Each role gets its own landing page: Organisers go to their dashboard
        return CurrentRole == Roles.Organiser
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Home");
    }
}
