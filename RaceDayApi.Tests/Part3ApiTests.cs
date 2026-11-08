using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDayApi.Tests;

/// <summary>
/// Tests for the API additions the MVC front end depends on: enrolment counts,
/// participant names, total finishers, upcoming-only filtering, event types and
/// safe deletes.
/// </summary>
public class Part3ApiTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public Part3ApiTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient organiser, int eventId, int categoryId)> SeedEventWithCategoryAsync()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);
        return (organiser, eventId, categoryId);
    }

    private async Task<(HttpClient client, int enrolmentId)> EnrolNewParticipantAsync(int eventId, int categoryId)
    {
        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (participant, json.GetProperty("enrolmentId").GetInt32());
    }

    [Fact]
    public async Task GetEventTypes_ReturnsRunWalkCycle()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/eventtypes");
        var types = await response.Content.ReadFromJsonAsync<List<JsonElement>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, types!.Count);
    }

    [Fact]
    public async Task GetEvents_UpcomingOnly_ExcludesPastEvents()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");

        var pastName = $"Past {Guid.NewGuid():N}";
        var futureName = $"Future {Guid.NewGuid():N}";

        await organiser.PostAsJsonAsync("/api/events", new
        {
            name = pastName, description = "old", eventDate = DateTime.UtcNow.AddDays(-30),
            location = "Cape Town", distanceKm = 5m, eventTypeId = 1
        });
        await organiser.PostAsJsonAsync("/api/events", new
        {
            name = futureName, description = "new", eventDate = DateTime.UtcNow.AddDays(30),
            location = "Cape Town", distanceKm = 5m, eventTypeId = 1
        });

        var events = await organiser.GetFromJsonAsync<List<JsonElement>>("/api/events?upcomingOnly=true");
        var names = events!.Select(e => e.GetProperty("name").GetString()).ToList();

        Assert.Contains(futureName, names);
        Assert.DoesNotContain(pastName, names);
    }

    [Fact]
    public async Task CreateEvent_InvalidDistance_ReturnsBadRequest()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Bad distance", description = "x", eventDate = DateTime.UtcNow.AddDays(5),
            location = "Cape Town", distanceKm = 0m, eventTypeId = 1
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEvents_IncludesEnrolmentCount()
    {
        var (organiser, eventId, categoryId) = await SeedEventWithCategoryAsync();
        await EnrolNewParticipantAsync(eventId, categoryId);
        await EnrolNewParticipantAsync(eventId, categoryId);

        var mine = await organiser.GetFromJsonAsync<List<JsonElement>>("/api/events/mine");

        Assert.Single(mine!);
        Assert.Equal(2, mine![0].GetProperty("enrolmentCount").GetInt32());
    }

    [Fact]
    public async Task GetEnrolmentsForEvent_IncludesParticipantName()
    {
        var (organiser, eventId, categoryId) = await SeedEventWithCategoryAsync();
        await EnrolNewParticipantAsync(eventId, categoryId);

        var enrolments = await organiser.GetFromJsonAsync<List<JsonElement>>($"/api/events/{eventId}/enrolments");

        Assert.Single(enrolments!);
        Assert.Equal("Test Participant", enrolments![0].GetProperty("participantName").GetString());
        Assert.Equal("Pending", enrolments[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetMyResults_IncludesTotalFinishers()
    {
        var (organiser, eventId, categoryId) = await SeedEventWithCategoryAsync();
        var (participantOne, enrolmentOne) = await EnrolNewParticipantAsync(eventId, categoryId);
        var (_, enrolmentTwo) = await EnrolNewParticipantAsync(eventId, categoryId);

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentOne}/result", new { finishTime = "01:30:00", finishPosition = 1 });
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentTwo}/result", new { finishTime = "01:45:00", finishPosition = 2 });

        var results = await participantOne.GetFromJsonAsync<List<JsonElement>>("/api/users/me/results");

        Assert.Single(results!);
        Assert.Equal(1, results![0].GetProperty("finishPosition").GetInt32());
        Assert.Equal(2, results[0].GetProperty("totalFinishers").GetInt32());
    }

    [Fact]
    public async Task CaptureResult_InvalidPosition_ReturnsBadRequest()
    {
        var (organiser, eventId, categoryId) = await SeedEventWithCategoryAsync();
        var (_, enrolmentId) = await EnrolNewParticipantAsync(eventId, categoryId);

        var response = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "01:30:00", finishPosition = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_ReturnsConflict()
    {
        var (organiser, eventId, categoryId) = await SeedEventWithCategoryAsync();
        await EnrolNewParticipantAsync(eventId, categoryId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_ReturnsConflict()
    {
        var (organiser, eventId, categoryId) = await SeedEventWithCategoryAsync();
        await EnrolNewParticipantAsync(eventId, categoryId);

        var response = await organiser.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
