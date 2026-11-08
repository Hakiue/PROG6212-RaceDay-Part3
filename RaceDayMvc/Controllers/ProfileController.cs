using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;
using RaceDayMvc.Models;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Controllers;

[RequireRole(Roles.Participant)]
public class ProfileController : AppController
{
    private readonly IRaceDayApi _api;

    public ProfileController(IRaceDayApi api) => _api = api;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMeAsync();

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (!result.IsSuccess) return ErrorPage(result.Error);

        return View(ToViewModel(result.Data!));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Re-load the display-only fields (they aren't posted back)
            var current = await _api.GetMeAsync();
            if (current.IsSuccess)
            {
                model.Email = current.Data!.Email;
                model.ProfilePictureUrl = current.Data.ProfilePictureUrl;
            }

            return View("Index", model);
        }

        var phone = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        var result = await _api.UpdateMeAsync(new UpdateProfileRequest(model.FirstName.Trim(), model.LastName.Trim(), phone));

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (!result.IsSuccess)
        {
            Failure(result.Error ?? "We couldn't save your details.");
            return RedirectToAction(nameof(Index));
        }

        // Keep the name in the menu up to date
        HttpContext.Session.SetString(SessionKeys.DisplayName, $"{result.Data!.FirstName} {result.Data.LastName}".Trim());
        Success("Your details have been saved.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            Failure("Please choose an image to upload.");
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.UploadProfilePictureAsync(picture);

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (result.IsSuccess)
            Success("Your profile picture has been updated.");
        else
            Failure(result.Error ?? "We couldn't upload that picture.");

        return RedirectToAction(nameof(Index));
    }

    private static ProfileViewModel ToViewModel(UserProfile profile) => new()
    {
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        PhoneNumber = profile.PhoneNumber,
        Email = profile.Email,
        ProfilePictureUrl = profile.ProfilePictureUrl
    };
}
