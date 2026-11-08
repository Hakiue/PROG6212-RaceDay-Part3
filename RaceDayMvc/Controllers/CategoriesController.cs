using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;
using RaceDayMvc.Models;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Controllers;

/// <summary>Organiser: add, edit and remove the categories (age or distance groups) of one of their events.</summary>
[RequireRole(Roles.Organiser)]
public class CategoriesController : AppController
{
    private readonly IRaceDayApi _api;

    public CategoriesController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index(int eventId)
    {
        var page = await LoadPageAsync(eventId, new CategoryFormViewModel { EventId = eventId });
        return page.Result ?? View(page.Model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryFormViewModel form)
    {
        ValidateAges(form);

        if (!ModelState.IsValid)
        {
            var page = await LoadPageAsync(form.EventId, form);
            return page.Result ?? View("Index", page.Model);
        }

        var result = await _api.CreateCategoryAsync(form.EventId, ToRequest(form));

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (result.IsSuccess)
            Success($"Category '{result.Data!.Name}' added.");
        else
            Failure(result.Error ?? "We couldn't add that category.");

        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int eventId, int id)
    {
        var categories = await _api.GetCategoriesAsync(eventId);
        if (!categories.IsSuccess) return categories.IsNotFound ? NotFound() : ErrorPage(categories.Error);

        var category = categories.Data!.FirstOrDefault(c => c.CategoryId == id);
        if (category is null) return NotFound();

        var ev = await _api.GetEventAsync(eventId);
        if (!ev.IsSuccess) return ev.IsNotFound ? NotFound() : ErrorPage(ev.Error);
        if (ev.Data!.OrganiserId != CurrentUserId) return RedirectToAction("AccessDenied", "Account");

        ViewData["EventName"] = ev.Data.Name;
        return View(new CategoryFormViewModel
        {
            EventId = eventId,
            CategoryId = category.CategoryId,
            Name = category.Name,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(CategoryFormViewModel form)
    {
        ValidateAges(form);

        if (!ModelState.IsValid || form.CategoryId is null)
        {
            var ev = await _api.GetEventAsync(form.EventId);
            ViewData["EventName"] = ev.IsSuccess ? ev.Data!.Name : "this event";
            return View(form);
        }

        var result = await _api.UpdateCategoryAsync(form.CategoryId.Value, ToRequest(form));

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (result.IsSuccess)
            Success("Category updated.");
        else
            Failure(result.Error ?? "We couldn't save that category.");

        return RedirectToAction(nameof(Index), new { eventId = form.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int eventId, int id)
    {
        var result = await _api.DeleteCategoryAsync(id);

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (result.IsSuccess)
            Success("Category removed.");
        else if (result.IsConflict)
            Failure("Participants have already entered in this category, so it can't be removed.");
        else
            Failure(result.Error ?? "We couldn't remove that category.");

        return RedirectToAction(nameof(Index), new { eventId });
    }

    // ---------- helpers ----------

    private void ValidateAges(CategoryFormViewModel form)
    {
        if (form.MinAge is int min && form.MaxAge is int max && min > max)
        {
            ModelState.AddModelError(nameof(form.MaxAge), "The maximum age can't be lower than the minimum age.");
        }
    }

    private static CategoryRequest ToRequest(CategoryFormViewModel f) =>
        new(f.Name.Trim(), f.MinAge, f.MaxAge, f.DistanceKm);

    private async Task<(IActionResult? Result, CategoriesViewModel Model)> LoadPageAsync(int eventId, CategoryFormViewModel form)
    {
        var model = new CategoriesViewModel { Form = form };

        var ev = await _api.GetEventAsync(eventId);
        if (!ev.IsSuccess)
            return (ev.IsNotFound ? NotFound() : ErrorPage(ev.Error), model);

        if (ev.Data!.OrganiserId != CurrentUserId)
            return (RedirectToAction("AccessDenied", "Account"), model);

        var categories = await _api.GetCategoriesAsync(eventId);
        var expired = HandleSessionExpired(categories);
        if (expired is not null) return (expired, model);

        if (!categories.IsSuccess) return (ErrorPage(categories.Error), model);

        model.Event = ev.Data;
        model.Categories = categories.Data!;
        model.Form.EventId = eventId;
        return (null, model);
    }
}
