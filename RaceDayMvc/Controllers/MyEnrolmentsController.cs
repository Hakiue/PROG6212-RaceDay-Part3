using Microsoft.AspNetCore.Mvc;
using RaceDayMvc.Infrastructure;

namespace RaceDayMvc.Controllers;

[RequireRole(Roles.Participant)]
public class MyEnrolmentsController : AppController
{
    private readonly IRaceDayApi _api;

    public MyEnrolmentsController(IRaceDayApi api) => _api = api;

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetMyEnrolmentsAsync();

        var expired = HandleSessionExpired(result);
        if (expired is not null) return expired;

        if (!result.IsSuccess) return ErrorPage(result.Error);

        return View(result.Data!);
    }
}
