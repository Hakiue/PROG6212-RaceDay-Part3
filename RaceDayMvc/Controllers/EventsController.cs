using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Controllers;

public class EventsController : AppController
{
    private readonly IRaceDayApi _api;

    public EventsController(IRaceDayApi api) => _api = api;

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (!ev.IsSuccess)
        {
            return ev.IsNotFound ? NotFound() : ErrorPage(ev.Error);
        }

        var categories = await _api.GetCategoriesAsync(id);

        // Participants: find out whether they've already entered this event
        Models.EnrolmentItem? existing = null;
        if (CurrentRole == Roles.Participant)
        {
            var mine = await _api.GetMyEnrolmentsAsync();
            var expired = HandleSessionExpired(mine);
            if (expired is not null) return expired;

            if (mine.IsSuccess)
            {
                existing = mine.Data!.FirstOrDefault(e => e.EventId == id);
            }
        }

        return View(new EventDetailsViewModel
        {
            Event = ev.Data!,
            Categories = categories.IsSuccess ? categories.Data! : new List<Models.CategoryItem>(),
            ExistingEnrolment = existing
        });
    }

    [HttpPost]
    [RequireRole(Roles.Participant)]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (categoryId is null)
        {
            Failure("Please choose a category before entering.");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        var result = await _api.EnrolAsync(eventId, categoryId.Value);

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (!result.IsSuccess)
        {
            Failure(result.IsConflict ? "You've already entered this event." : result.Error ?? "We couldn't enter you in this event.");
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        Success("You're in! Your entry is pending until the organiser confirms it.");
        return RedirectToAction("Index", "MyEnrolments");
    }
}
