using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Common;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/users")]
[RequireRole]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blob;

    public UsersController(RaceDayDbContext context, IBlobStorageService blob)
    {
        _context = context;
        _blob = blob;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    private UserProfileDto ToProfile(User user) => new(
        user.UserId, user.FirstName, user.LastName, user.Email, user.Role,
        user.PhoneNumber, _blob.GetReadUrl(user.ProfilePictureUrl));

    /// <summary>
    /// Returns the profile of the currently logged-in user.
    /// </summary>
    /// <response code="200">Returns the user's profile.</response>
    /// <response code="401">No active session.</response>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = await _context.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        return Ok(ToProfile(user));
    }

    /// <summary>
    /// Updates the logged-in user's own first name, last name, and phone number.
    /// </summary>
    /// <response code="200">Profile updated.</response>
    /// <response code="400">First or last name was empty.</response>
    /// <response code="401">No active session.</response>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First name and last name are required." });

        var user = await _context.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;

        await _context.SaveChangesAsync();

        return Ok(ToProfile(user));
    }

    /// <summary>
    /// Uploads (or replaces) the logged-in Participant's profile picture. The image is stored
    /// in Azure Blob Storage and its URL is saved on the user. Accepts JPEG, PNG, WebP or GIF up to 5 MB.
    /// </summary>
    /// <param name="file">The image file (multipart form field named "file").</param>
    /// <response code="200">Picture stored. Returns a browser-loadable image URL.</response>
    /// <response code="400">No file, file too large, or not an allowed image type.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in user is not a Participant.</response>
    /// <response code="503">Image storage is not configured on the server.</response>
    [HttpPut("me/profile-picture")]
    [RequireRole("Participant")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var error = ImageUpload.Validate(file);
        if (error is not null) return BadRequest(new { message = error });

        if (!_blob.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured on the server." });

        var user = await _context.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        await using var stream = file.OpenReadStream();
        var url = await _blob.UploadImageAsync(stream, file.ContentType, "profiles");

        var oldUrl = user.ProfilePictureUrl;
        user.ProfilePictureUrl = url;
        await _context.SaveChangesAsync();
        await _blob.DeleteAsync(oldUrl);

        return Ok(new { profilePictureUrl = _blob.GetReadUrl(url) });
    }
}
