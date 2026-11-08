using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Common;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context) => _context = context;

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    private static ResultDto ToDto(Result r, int totalFinishers)
    {
        var enrolment = r.Enrolment;
        var participant = enrolment?.Participant;

        return new ResultDto(
            r.ResultId,
            r.EnrolmentId,
            participant is null ? string.Empty : $"{participant.FirstName} {participant.LastName}",
            enrolment?.EventId ?? 0,
            enrolment?.Event?.Name ?? string.Empty,
            enrolment?.Event?.EventDate ?? default,
            enrolment?.Category?.Name ?? string.Empty,
            r.FinishTime,
            r.FinishPosition,
            totalFinishers);
    }

    private IQueryable<Result> WithDetails() => _context.Results
        .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
        .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
        .Include(r => r.Enrolment).ThenInclude(e => e!.Participant);

    private async Task<Dictionary<int, int>> GetTotalsAsync(IEnumerable<int> eventIds)
    {
        var ids = eventIds.Distinct().ToList();

        return await _context.Results
            .Where(r => ids.Contains(r.Enrolment!.EventId))
            .GroupBy(r => r.Enrolment!.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);
    }

    private async Task<List<ResultDto>> ToDtosAsync(List<Result> results)
    {
        var totals = await GetTotalsAsync(results.Select(r => r.Enrolment!.EventId));
        return results.Select(r => ToDto(r, totals.GetValueOrDefault(r.Enrolment!.EventId))).ToList();
    }

    private static bool IsInvalid(ResultCreateRequest request) =>
        request.FinishPosition < 1 || request.FinishTime <= TimeSpan.Zero;

    /// <summary>
    /// Captures the finish time and finishing position for a Participant's enrolment, for an
    /// event owned by the logged-in Organiser.
    /// </summary>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Finish time must be above zero and the position 1 or more.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own this event.</response>
    /// <response code="404">No enrolment exists with that ID.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpPost("api/enrolments/{enrolmentId}/result")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> Create(int enrolmentId, ResultCreateRequest request)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Participant)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event!.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });
        if (IsInvalid(request)) return BadRequest(new { message = "Finish time must be above zero and the finishing position 1 or more." });
        if (enrolment.Result is not null) return Conflict(new { message = "A result has already been captured for this enrolment." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            RecordedByOrganiserId = CurrentUserId,
            FinishTime = request.FinishTime,
            FinishPosition = request.FinishPosition,
            RecordedAt = DateTime.UtcNow
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();
        result.Enrolment = enrolment;

        var totals = await GetTotalsAsync(new[] { enrolment.EventId });
        return StatusCode(StatusCodes.Status201Created, ToDto(result, totals.GetValueOrDefault(enrolment.EventId)));
    }

    /// <summary>
    /// Corrects an already-captured result for an event owned by the logged-in Organiser.
    /// </summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Finish time must be above zero and the position 1 or more.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own this event.</response>
    /// <response code="404">No result exists with that ID.</response>
    [HttpPut("api/results/{id}")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> Update(int id, ResultCreateRequest request)
    {
        var result = await WithDetails().FirstOrDefaultAsync(r => r.ResultId == id);

        if (result is null) return NotFound();
        if (result.Enrolment!.Event!.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });
        if (IsInvalid(request)) return BadRequest(new { message = "Finish time must be above zero and the finishing position 1 or more." });

        result.FinishTime = request.FinishTime;
        result.FinishPosition = request.FinishPosition;
        await _context.SaveChangesAsync();

        var totals = await GetTotalsAsync(new[] { result.Enrolment.EventId });
        return Ok(ToDto(result, totals.GetValueOrDefault(result.Enrolment.EventId)));
    }

    /// <summary>
    /// Returns the logged-in Participant's personal race history: event name, date, category,
    /// finish time, finishing position and the total number of finishers in each event
    /// (so the position can be shown as, for example, 47th out of 312).
    /// </summary>
    /// <response code="200">Returns the Participant's results (empty array if none yet).</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in user is not a Participant.</response>
    [HttpGet("api/users/me/results")]
    [RequireRole("Participant")]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = CurrentUserId;
        var results = await WithDetails()
            .Where(r => r.Enrolment!.ParticipantId == userId)
            .ToListAsync();

        var dtos = await ToDtosAsync(results);
        return Ok(dtos.OrderByDescending(r => r.EventDate));
    }

    /// <summary>
    /// Lists all published results for a specific event owned by the logged-in Organiser,
    /// ordered by finishing position.
    /// </summary>
    /// <response code="200">Returns the results for this event.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpGet("api/events/{eventId}/results")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var results = await WithDetails()
            .Where(r => r.Enrolment!.EventId == eventId)
            .ToListAsync();

        var dtos = await ToDtosAsync(results);
        return Ok(dtos.OrderBy(r => r.FinishPosition));
    }
}
