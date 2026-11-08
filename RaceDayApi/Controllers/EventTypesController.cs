using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.DTOs;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/eventtypes")]
public class EventTypesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventTypesController(RaceDayDbContext context) => _context = context;

    /// <summary>
    /// Lists the available event types (Run, Walk, Cycle). Public — used to fill the
    /// event type dropdown when creating or filtering events.
    /// </summary>
    /// <response code="200">Returns the event types.</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var types = await _context.EventTypes
            .OrderBy(t => t.EventTypeId)
            .Select(t => new EventTypeDto(t.EventTypeId, t.TypeName))
            .ToListAsync();

        return Ok(types);
    }
}
