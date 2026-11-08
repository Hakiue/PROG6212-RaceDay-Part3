using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;
using RaceDayMvc.ViewModels;

namespace RaceDayMvc.Controllers;

/// <summary>Organiser home page: a summary of the events they have created.</summary>
[RequireRole(Roles.Organiser)]
public class DashboardController : AppController
{
    private readonly IRaceDayApi _api;

    public DashboardController(IRaceDayApi api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEventsAsync();

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (!result.IsSuccess) return ErrorPage(result.Error);

        return View(new DashboardViewModel { Events = result.Data! });
    }
}
