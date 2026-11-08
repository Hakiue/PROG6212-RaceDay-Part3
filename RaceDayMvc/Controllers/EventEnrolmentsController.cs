using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Controllers;

/// <summary>Organiser: see everyone who entered one of their events, and confirm or cancel entries.</summary>
[RequireRole(Roles.Organiser)]
public class EventEnrolmentsController : AppController
{
    private static readonly string[] AllowedStatuses = { "Pending", "Confirmed", "Cancelled" };

    private readonly IRaceDayApi _api;

    public EventEnrolmentsController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await _api.GetEventAsync(eventId);
        if (!ev.IsSuccess) return ev.IsNotFound ? NotFound() : ErrorPage(ev.Error);

        if (ev.Data!.OrganiserId != CurrentUserId) return RedirectToAction("AccessDenied", "Account");

        var enrolments = await _api.GetEventEnrolmentsAsync(eventId);

        var expired = HandleSessionExpired(enrolments);
        if (expired is not null) return expired;

        if (!enrolments.IsSuccess) return ErrorPage(enrolments.Error);

        return View(new EventEnrolmentsViewModel
        {
            Event = ev.Data,
            Enrolments = enrolments.Data!.OrderBy(e => e.ParticipantName).ToList()
        });
    }

    [HttpPost]
    public async Task<IActionResult> SetStatus(int eventId, int enrolmentId, string status)
    {
        if (!AllowedStatuses.Contains(status))
        {
            Failure("That isn't a valid status.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var result = await _api.UpdateEnrolmentStatusAsync(enrolmentId, status);

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (result.IsSuccess)
            Success($"{result.Data!.ParticipantName}'s entry is now {status.ToLowerInvariant()}.");
        else
            Failure(result.Error ?? "We couldn't update that entry.");

        return RedirectToAction(nameof(Index), new { eventId });
    }
}
