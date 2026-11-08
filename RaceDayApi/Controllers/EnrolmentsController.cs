using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Common;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context) => _context = context;

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    private static EnrolmentDto ToDto(Enrolment en) => new(
        en.EnrolmentId,
        en.ParticipantId,
        en.Participant is null ? string.Empty : $"{en.Participant.FirstName} {en.Participant.LastName}",
        en.Participant?.Email ?? string.Empty,
        en.EventId,
        en.Event?.Name ?? string.Empty,
        en.Event?.EventDate ?? default,
        en.CategoryId,
        en.Category?.Name ?? string.Empty,
        en.EnrolmentDate,
        en.Status);

    private IQueryable<Enrolment> WithDetails() => _context.Enrolments
        .Include(e => e.Event)
        .Include(e => e.Category)
        .Include(e => e.Participant);

    /// <summary>
    /// Enrols the logged-in Participant into an event under a chosen category.
    /// </summary>
    /// <response code="201">Enrolment created.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in user is not a Participant.</response>
    /// <response code="404">Event or category does not exist.</response>
    /// <response code="409">Already enrolled in this event.</response>
    [HttpPost("api/events/{eventId}/enrolments")]
    [RequireRole("Participant")]
    public async Task<IActionResult> Create(int eventId, EnrolmentCreateRequest request)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId);
        if (category is null) return NotFound(new { message = "Category not found for this event." });

        var alreadyEnrolled = await _context.Enrolments.AnyAsync(e => e.EventId == eventId && e.ParticipantId == CurrentUserId);
        if (alreadyEnrolled) return Conflict(new { message = "Already enrolled in this event." });

        var enrolment = new Enrolment
        {
            ParticipantId = CurrentUserId,
            EventId = eventId,
            CategoryId = request.CategoryId,
            EnrolmentDate = DateTime.UtcNow,
            Status = "Pending"
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();
        await _context.Entry(enrolment).Reference(e => e.Event).LoadAsync();
        await _context.Entry(enrolment).Reference(e => e.Category).LoadAsync();
        await _context.Entry(enrolment).Reference(e => e.Participant).LoadAsync();

        return StatusCode(StatusCodes.Status201Created, ToDto(enrolment));
    }

    /// <summary>
    /// Lists all events the logged-in Participant has enrolled in, with status.
    /// </summary>
    /// <response code="200">Returns the Participant's enrolments.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in user is not a Participant.</response>
    [HttpGet("api/users/me/enrolments")]
    [RequireRole("Participant")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = CurrentUserId;
        var enrolments = await WithDetails()
            .Where(e => e.ParticipantId == userId)
            .OrderByDescending(e => e.EnrolmentDate)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>
    /// Lists all Participants enrolled in a specific event owned by the logged-in Organiser,
    /// including each Participant's name, selected category and enrolment status.
    /// </summary>
    /// <response code="200">Returns the enrolments for this event.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpGet("api/events/{eventId}/enrolments")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var enrolments = await WithDetails()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.Participant!.LastName)
            .ThenBy(e => e.Participant!.FirstName)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>
    /// Updates an enrolment's status (e.g. Confirmed/Cancelled) for an event owned by the
    /// logged-in Organiser.
    /// </summary>
    /// <response code="200">Status updated.</response>
    /// <response code="400">Status value was not one of Pending/Confirmed/Cancelled.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own this event.</response>
    /// <response code="404">No enrolment exists with that ID.</response>
    [HttpPut("api/enrolments/{id}/status")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> UpdateStatus(int id, EnrolmentStatusUpdateRequest request)
    {
        var enrolment = await WithDetails().FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment is null) return NotFound();
        if (enrolment.Event!.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var validStatuses = new[] { "Pending", "Confirmed", "Cancelled" };
        if (!validStatuses.Contains(request.Status)) return BadRequest(new { message = "Invalid status." });

        enrolment.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(ToDto(enrolment));
    }
}
