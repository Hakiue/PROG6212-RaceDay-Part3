using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDayApi.Common;

/// <summary>
/// Enforces session-based authentication and, optionally, a specific role.
/// [RequireRole]              -> any logged-in user (401 if no session)
/// [RequireRole("Organiser")] -> must be logged in AND have the Organiser role (403 otherwise)
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public RequireRoleAttribute(string? role = null)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);

        if (userId is null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "No active session. Please log in." });
            return;
        }

        if (_role is not null)
        {
            var role = session.GetString(SessionKeys.Role);
            if (!string.Equals(role, _role, StringComparison.OrdinalIgnoreCase))
            {
                context.Result = new ObjectResult(new { message = $"This action requires the {_role} role." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
