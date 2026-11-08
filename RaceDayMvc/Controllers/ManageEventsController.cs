using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;
using RaceDayMvc.Models;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Controllers;

/// <summary>Organiser: list, create, edit and delete their events (with banner upload via the API).</summary>
[RequireRole(Roles.Organiser)]
public class ManageEventsController : AppController
{
    private readonly IRaceDayApi _api;

    public ManageEventsController(IRaceDayApi api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEventsAsync();

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (!result.IsSuccess) return ErrorPage(result.Error);

        return View(result.Data!.OrderByDescending(e => e.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new EventFormViewModel { EventDate = DateTime.Today.AddDays(30).AddHours(7) };
        return await LoadTypesAndShowForm(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model, IFormFile? banner)
    {
        if (!ModelState.IsValid) return await LoadTypesAndShowForm(model);

        var created = await _api.CreateEventAsync(ToRequest(model));

        var expired = HandleSessionExpired(created);
        if (expired is not null) return expired;

        if (!created.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, created.Error ?? "We couldn't create the event.");
            return await LoadTypesAndShowForm(model);
        }

        var eventId = created.Data!.EventId;
        Success($"'{created.Data.Name}' has been created.");
        await UploadBannerIfProvidedAsync(eventId, banner);

        // Next logical step: add categories so participants can enter
        return RedirectToAction("Index", "Categories", new { eventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var ev = await _api.GetEventAsync(id);
        if (!ev.IsSuccess) return ev.IsNotFound ? NotFound() : ErrorPage(ev.Error);

        if (ev.Data!.OrganiserId != CurrentUserId) return RedirectToAction("AccessDenied", "Account");

        var model = new EventFormViewModel
        {
            EventId = ev.Data.EventId,
            Name = ev.Data.Name,
            Description = ev.Data.Description,
            EventDate = ev.Data.EventDate,
            Location = ev.Data.Location,
            DistanceKm = ev.Data.DistanceKm,
            EventTypeId = ev.Data.EventTypeId,
            BannerImageUrl = ev.Data.BannerImageUrl
        };
        return await LoadTypesAndShowForm(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model, IFormFile? banner)
    {
        model.EventId = id;

        if (!ModelState.IsValid) return await LoadTypesAndShowForm(model, reloadBanner: true);

        var updated = await _api.UpdateEventAsync(id, ToRequest(model));

        var expired = HandleSessionExpired(updated);
        if (expired is not null) return expired;

        if (updated.IsForbidden) return RedirectToAction("AccessDenied", "Account");
        if (updated.IsNotFound) return NotFound();

        if (!updated.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, updated.Error ?? "We couldn't save your changes.");
            return await LoadTypesAndShowForm(model, reloadBanner: true);
        }

        Success("Your changes have been saved.");
        await UploadBannerIfProvidedAsync(id, banner);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteEventAsync(id);

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (result.IsSuccess)
            Success("The event has been deleted.");
        else if (result.IsConflict)
            Failure("This event already has entries, so it can't be deleted. Cancel the entries first if you need to remove it.");
        else
            Failure(result.Error ?? "We couldn't delete that event.");

        return RedirectToAction(nameof(Index));
    }

    // ---------- helpers ----------

    private async Task<IActionResult> LoadTypesAndShowForm(EventFormViewModel model, bool reloadBanner = false)
    {
        var types = await _api.GetEventTypesAsync();
        var expired = HandleSessionExpired(types);
        if (expired is not null) return expired;

        if (!types.IsSuccess) return ErrorPage(types.Error);
        model.EventTypes = types.Data!;

        // The banner URL isn't posted back, so fetch it again when the form is re-shown
        if (reloadBanner && model.EventId is int id)
        {
            var ev = await _api.GetEventAsync(id);
            if (ev.IsSuccess) model.BannerImageUrl = ev.Data!.BannerImageUrl;
        }

        return View("Form", model);
    }

    private static EventRequest ToRequest(EventFormViewModel m) => new(
        m.Name.Trim(),
        string.IsNullOrWhiteSpace(m.Description) ? null : m.Description.Trim(),
        m.EventDate!.Value,
        m.Location.Trim(),
        m.DistanceKm!.Value,
        m.EventTypeId);

    /// <summary>Sends the banner to the API (which stores it in Azure Blob Storage). The MVC app never talks to Azure.</summary>
    private async Task UploadBannerIfProvidedAsync(int eventId, IFormFile? banner)
    {
        if (banner is null || banner.Length == 0) return;

        var upload = await _api.UploadBannerAsync(eventId, banner);
        if (!upload.IsSuccess)
        {
            Failure($"The event was saved, but the banner couldn't be uploaded: {upload.Error} You can add it again by editing the event.");
        }
    }
}
