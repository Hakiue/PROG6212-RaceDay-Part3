using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Common;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AuthController(RaceDayDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// Registers a new user as either an Organiser or a Participant. The password is hashed
    /// before being stored.
    /// </summary>
    /// <response code="201">User created successfully.</response>
    /// <response code="400">Role was missing or not 'Organiser'/'Participant'.</response>
    /// <response code="409">A user with this email is already registered.</response>
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!string.Equals(request.Role, "Organiser", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Role, "Participant", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });
        }

        var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            return Conflict(new { message = "A user with this email is already registered." });
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = request.Role,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var response = new AuthResponse(user.UserId, user.FirstName, user.LastName, user.Email, user.Role);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Authenticates a user's credentials and starts a session storing their user ID and role.
    /// </summary>
    /// <response code="200">Login successful, session started.</response>
    /// <response code="401">Email or password was incorrect.</response>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        return Ok(new AuthResponse(user.UserId, user.FirstName, user.LastName, user.Email, user.Role));
    }

    /// <summary>
    /// Ends the current user's session.
    /// </summary>
    /// <response code="200">Session cleared.</response>
    /// <response code="401">No active session.</response>
    [HttpPost("logout")]
    [RequireRole]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Session cleared." });
    }
}
