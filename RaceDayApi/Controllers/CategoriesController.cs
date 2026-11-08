using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Common;
using RaceDayApi.Data;
using RaceDayApi.DTOs;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context) => _context = context;

    private int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

    private static CategoryDto ToDto(Category c) => new(c.CategoryId, c.EventId, c.Name, c.MinAge, c.MaxAge, c.DistanceKm);

    /// <summary>
    /// Lists all categories available for a given event. Public — no login required.
    /// </summary>
    /// <response code="200">Returns the list of categories.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpGet("api/events/{eventId}/categories")]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound();

        var categories = await _context.Categories.Where(c => c.EventId == eventId).ToListAsync();
        return Ok(categories.Select(ToDto));
    }

    /// <summary>
    /// Adds a new age/distance category to an event owned by the logged-in Organiser.
    /// </summary>
    /// <response code="201">Category created.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own this event.</response>
    /// <response code="404">No event exists with that ID.</response>
    [HttpPost("api/events/{eventId}/categories")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> Create(int eventId, CategoryCreateRequest request)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            DistanceKm = request.DistanceKm
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetForEvent), new { eventId }, ToDto(category));
    }

    /// <summary>
    /// Updates a category belonging to an event owned by the logged-in Organiser.
    /// </summary>
    /// <response code="200">Category updated.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own the parent event.</response>
    /// <response code="404">No category exists with that ID.</response>
    [HttpPut("api/categories/{id}")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> Update(int id, CategoryCreateRequest request)
    {
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event!.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        category.Name = request.Name;
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.DistanceKm = request.DistanceKm;

        await _context.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    /// <summary>
    /// Deletes a category from an event owned by the logged-in Organiser.
    /// </summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="409">The category has enrolments and can't be deleted.</response>
    /// <response code="401">No active session.</response>
    /// <response code="403">Logged in Organiser does not own the parent event.</response>
    /// <response code="404">No category exists with that ID.</response>
    [HttpDelete("api/categories/{id}")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event!.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.CategoryId == id);
        if (hasEnrolments) return Conflict(new { message = "This category has enrolments and can't be deleted." });

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
