using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;
using RaceDayMvc.Models;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Controllers;

public class HomeController : AppController
{
    private readonly IRaceDayApi _api;

    public HomeController(IRaceDayApi api) => _api = api;

    public async Task<IActionResult> Index(string? eventType, string? location, bool upcomingOnly = true)
    {
        var events = await _api.GetEventsAsync(eventType, location, upcomingOnly);
        var types = await _api.GetEventTypesAsync();

        var model = new EventListViewModel
        {
            Events = events.IsSuccess ? events.Data! : new List<EventSummary>(),
            EventTypes = types.IsSuccess ? types.Data! : new List<EventTypeItem>(),
            EventType = eventType,
            Location = location,
            UpcomingOnly = upcomingOnly,
            LoadError = events.IsSuccess ? null : events.Error
        };

        return View(model);
    }

    [IgnoreAntiforgeryToken]
    public IActionResult Error() =>
        View("Error", new ErrorViewModel
        {
            Code = 500,
            Title = "Something went wrong",
            Message = "Something went wrong on our side. Please try again in a moment."
        });

    [IgnoreAntiforgeryToken]
    public IActionResult Status(int code)
    {
        var model = code switch
        {
            404 => new ErrorViewModel { Code = 404, Title = "We can't find that page", Message = "The page you're looking for doesn't exist or has moved." },
            403 => new ErrorViewModel { Code = 403, Title = "Not allowed", Message = "You don't have permission to see that page." },
            _ => new ErrorViewModel { Code = code, Title = "Something went wrong", Message = "Something went wrong. Please try again." }
        };

        Response.StatusCode = code;
        return View("Error", model);
    }
}
