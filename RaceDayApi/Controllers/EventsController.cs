using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Common;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;
using RaceDayApi.Services;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blob;

    public EventsController(RaceDayDbContext context, IBlobStorageService blob)
    {
        _context = context;
        _blob = blob;
    }

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    private static IQueryable<EventDto> ProjectToDto(IQueryable<Event> query) =>
        query.Select(e => new EventDto(
            e.EventId, e.OrganiserId, e.Name, e.Description, e.EventDate, e.Location, e.DistanceKm,
            e.EventTypeId, e.EventType!.TypeName, e.BannerImageUrl, e.Enrolments.Count));

    private EventDto WithReadUrl(EventDto dto) =>
        dto with { BannerImageUrl = _blob.GetReadUrl(dto.BannerImageUrl) };

    private static string? ValidateEventFields(string name, string location, decimal distanceKm) =>
        string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(location) || distanceKm <= 0
            ? "Name, location and a distance greater than 0 are required."
            : null;

    /// <summary>
    /// Lists events, with optional filtering by event type, location, or upcoming dates only.
    /// Public — no login required.
    /// </summary>
    /// <param name="eventType">Event type name: Run, Walk or Cycle.</param>
    /// <param name="location">Partial location text to match.</param>
    /// <param name="upcomingOnly">When true, only events happening today or later are returned.</param>
    /// <response code="200">Returns the list of matching events.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? eventType,
        [FromQuery] string? location,
        [FromQuery] bool upcomingOnly = false)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType!.TypeName == eventType);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(e => e.Location.Contains(location));

        if (upcomingOnly)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(e => e.EventDate >= today);
        }

        var events = await ProjectToDto(query.OrderBy(e => e.EventDate)).ToListAsync();
        return Ok(events.Select(WithReadUrl));
    }

    /// <summary>
    /// Lists all events created by the logged-in Organiser, including the number of
    /// enrolments for each (used for the dashboard).
    /// </summary>
    /// <response code="200">Returns the Organiser's own events.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in user is not an Organiser.</response>
    [HttpGet("mine")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> GetMine()
    {
        var userId = CurrentUserId;
        var query = _context.Events.Where(e => e.OrganiserId == userId).OrderBy(e => e.EventDate);

        var events = await ProjectToDto(query).ToListAsync();
        return Ok(events.Select(WithReadUrl));
    }

    /// <summary>
    /// Returns full detail for a single event. Public — no login required.
    /// </summary>
    /// <response code="200">Returns the event detail.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await ProjectToDto(_context.Events.Where(e => e.EventId == id)).FirstOrDefaultAsync();
        if (dto is null) return NotFound();

        return Ok(WithReadUrl(dto));
    }

    /// <summary>
    /// Creates a new event owned by the logged-in Organiser.
    /// </summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">A required field is missing, the distance is not above 0, or the EventTypeId does not exist.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in user is not an Organiser.</response>
    [HttpPost]
    [RequireRole("Organiser")]
    public async Task<IActionResult> Create(EventCreateRequest request)
    {
        var fieldError = ValidateEventFields(request.Name, request.Location, request.DistanceKm);
        if (fieldError is not null) return BadRequest(new { message = fieldError });

        var typeExists = await _context.EventTypes.AnyAsync(t => t.EventTypeId == request.EventTypeId);
        if (!typeExists) return BadRequest(new { message = "Invalid EventTypeId." });

        var ev = new Event
        {
            OrganiserId = CurrentUserId,
            EventTypeId = request.EventTypeId,
            Name = request.Name.Trim(),
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            CreatedAt = DateTime.UtcNow
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        var dto = await ProjectToDto(_context.Events.Where(e => e.EventId == ev.EventId)).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, WithReadUrl(dto));
    }

    /// <summary>
    /// Updates an event owned by the logged-in Organiser.
    /// </summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">A required field is missing, the distance is not above 0, or the EventTypeId does not exist.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpPut("{id}")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> Update(int id, EventUpdateRequest request)
    {
        var ev = await _context.Events.FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var fieldError = ValidateEventFields(request.Name, request.Location, request.DistanceKm);
        if (fieldError is not null) return BadRequest(new { message = fieldError });

        var typeExists = await _context.EventTypes.AnyAsync(t => t.EventTypeId == request.EventTypeId);
        if (!typeExists) return BadRequest(new { message = "Invalid EventTypeId." });

        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventTypeId = request.EventTypeId;

        await _context.SaveChangesAsync();

        var dto = await ProjectToDto(_context.Events.Where(e => e.EventId == id)).FirstAsync();
        return Ok(WithReadUrl(dto));
    }

    /// <summary>
    /// Deletes an event owned by the logged-in Organiser. Events that already have
    /// enrolments cannot be deleted.
    /// </summary>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    /// <response code="409">The event has enrolments and can't be deleted.</response>
    [HttpDelete("{id}")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var hasEnrolments = await _context.Enrolments.AnyAsync(en => en.EventId == id);
        if (hasEnrolments) return Conflict(new { message = "This event has enrolments and can't be deleted." });

        var bannerUrl = ev.BannerImageUrl;
        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        await _blob.DeleteAsync(bannerUrl);

        return NoContent();
    }

    /// <summary>
    /// Uploads (or replaces) the banner image for an event owned by the logged-in Organiser.
    /// The image is stored in Azure Blob Storage and its URL is saved on the event.
    /// Accepts JPEG, PNG, WebP or GIF up to 5 MB.
    /// </summary>
    /// <param name="id">The event ID.</param>
    /// <param name="file">The image file (multipart form field named "file").</param>
    /// <response code="200">Banner stored. Returns a browser-loadable image URL.</response>
    /// <response code="400">No file, file too large, or not an allowed image type.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    /// <response code="503">Image storage is not configured on the server.</response>
    [HttpPost("{id}/banner")]
    [RequireRole("Organiser")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var error = ImageUpload.Validate(file);
        if (error is not null) return BadRequest(new { message = error });

        if (!_blob.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not configured on the server." });

        await using var stream = file.OpenReadStream();
        var url = await _blob.UploadImageAsync(stream, file.ContentType, "events");

        var oldUrl = ev.BannerImageUrl;
        ev.BannerImageUrl = url;
        await _context.SaveChangesAsync();
        await _blob.DeleteAsync(oldUrl);

        return Ok(new { bannerImageUrl = _blob.GetReadUrl(url) });
    }
}
