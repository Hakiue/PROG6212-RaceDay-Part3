using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Models;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Infrastructure;

public abstract class AppController : Controller
{
    protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId);
    protected string? CurrentRole => HttpContext.Session.GetString(SessionKeys.Role);

    protected void StartSession(AuthResponse user)
    {
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);
        HttpContext.Session.SetString(SessionKeys.DisplayName, $"{user.FirstName} {user.LastName}".Trim());
    }

    /// <summary>
    /// If the API says the session has ended (it expires after 2 idle hours), clear ours and
    /// send the user to log in again. Returns null when the call was fine.
    /// </summary>
    protected IActionResult? HandleSessionExpired(ApiResult result)
    {
        if (!result.IsUnauthorized) return null;

        HttpContext.Session.Clear();
        TempData["Warning"] = "Your session has expired. Please log in again.";
        return RedirectToAction("Login", "Account");
    }

    protected void Success(string message) => TempData["Success"] = message;
    protected void Failure(string message) => TempData["Error"] = message;

    protected IActionResult ErrorPage(string? message, string title = "We hit a snag") =>
        View("Error", new ErrorViewModel
        {
            Title = title,
            Message = message ?? "Something went wrong. Please try again."
        });
}
