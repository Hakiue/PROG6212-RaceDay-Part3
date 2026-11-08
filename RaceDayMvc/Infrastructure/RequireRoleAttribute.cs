using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayMvc.Infrastructure;

/// <summary>
/// Protects MVC pages. [RequireRole] = any logged-in user; [RequireRole("Organiser")] = that
/// role only. Anonymous visitors are sent to the login page (and back afterwards); a logged-in
/// user with the wrong role is sent to an access-denied page. The API enforces the same rules
/// again on every call, so this is for a good experience, not the only line of defence.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : ActionFilterAttribute
{
    private readonly string? _role;

    public RequireRoleAttribute(string? role = null) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;

        if (session.GetInt32(SessionKeys.UserId) is null)
        {
            var request = context.HttpContext.Request;
            var returnUrl = $"{request.Path}{request.QueryString}";
            context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
            return;
        }

        if (_role is not null &&
            !string.Equals(session.GetString(SessionKeys.Role), _role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
        }
    }
}
