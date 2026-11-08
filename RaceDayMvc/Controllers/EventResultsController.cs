using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;
using RaceDayMvc.Models;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Controllers;

/// <summary>
/// Organiser: capture finish times and positions. A result is visible to the participant as soon
/// as it is saved, so "save" and "publish" are the same step.
/// </summary>
[RequireRole(Roles.Organiser)]
public class EventResultsController : AppController
{
    private readonly IRaceDayApi _api;

    public EventResultsController(IRaceDayApi api) => _api = api;

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

        var results = await _api.GetEventResultsAsync(eventId);
        if (!results.IsSuccess) return ErrorPage(results.Error);

        var byEnrolment = results.Data!.ToDictionary(r => r.EnrolmentId);

        // Cancelled entries can't have a result
        var rows = enrolments.Data!
            .Where(e => !string.Equals(e.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => byEnrolment.TryGetValue(e.EnrolmentId, out var r) ? r.FinishPosition : int.MaxValue)
            .ThenBy(e => e.ParticipantName)
            .Select(e => new ResultRowViewModel
            {
                Enrolment = e,
                Result = byEnrolment.TryGetValue(e.EnrolmentId, out var r) ? r : null
            })
            .ToList();

        return View(new EventResultsViewModel
        {
            Event = ev.Data,
            Rows = rows,
            NextPosition = results.Data!.Count == 0 ? 1 : results.Data.Max(r => r.FinishPosition) + 1
        });
    }

    [HttpPost]
    public async Task<IActionResult> Save(int eventId, int enrolmentId, int? resultId, string? finishTime, int? finishPosition)
    {
        if (!TryParseFinishTime(finishTime, out var time))
        {
            Failure("Enter the finish time as hours:minutes:seconds, for example 01:45:30.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        if (finishPosition is null or < 1)
        {
            Failure("The finishing position must be 1 or higher.");
            return RedirectToAction(nameof(Index), new { eventId });
        }

        var body = new ResultRequest(time, finishPosition.Value);
        var result = resultId is int existing
            ? await _api.UpdateResultAsync(existing, body)
            : await _api.CaptureResultAsync(enrolmentId, body);

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (result.IsSuccess)
            Success($"Result published for {result.Data!.ParticipantName}.");
        else
            Failure(result.Error ?? "We couldn't save that result.");

        return RedirectToAction(nameof(Index), new { eventId });
    }

    /// <summary>Accepts 1:45:30, 01:45:30 or 45:30 (minutes:seconds). Must be longer than zero.</summary>
    private static bool TryParseFinishTime(string? text, out TimeSpan time)
    {
        time = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var cleaned = text.Trim();
        // A bare "45:30" would be read as hours:minutes by TimeSpan, so treat two-part input as mm:ss
        if (cleaned.Count(c => c == ':') == 1) cleaned = "0:" + cleaned;

        return TimeSpan.TryParse(cleaned, CultureInfo.InvariantCulture, out time)
               && time > TimeSpan.Zero
               && time < TimeSpan.FromDays(1);
    }
}
