using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayApi.Tests;
using Xunit;

namespace RaceDayApi.Tests;

public class ResultsTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public ResultsTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient organiser, HttpClient participant, int enrolmentId)> SeedEnrolmentAsync()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);
        var categoryId = await TestHelpers.CreateCategoryAsync(organiser, eventId);

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");
        var enrolResponse = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<JsonElement>();
        var enrolmentId = enrolment.GetProperty("enrolmentId").GetInt32();

        return (organiser, participant, enrolmentId);
    }

    [Fact]
    public async Task CaptureResult_AsOwningOrganiser_ReturnsCreated()
    {
        var (organiser, _, enrolmentId) = await SeedEnrolmentAsync();

        var response = await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "01:45:00", finishPosition = 12 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_Twice_ReturnsConflict()
    {
        var (organiser, _, enrolmentId) = await SeedEnrolmentAsync();
        var body = new { finishTime = "01:45:00", finishPosition = 12 };

        var first = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", body);
        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_AsParticipant_ShowsCapturedResult()
    {
        var (organiser, participant, enrolmentId) = await SeedEnrolmentAsync();
        await organiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "01:45:00", finishPosition = 12 });

        var results = await participant.GetFromJsonAsync<List<JsonElement>>("/api/users/me/results");

        Assert.Single(results!);
        Assert.Equal(12, results![0].GetProperty("finishPosition").GetInt32());
    }

    [Fact]
    public async Task CaptureResult_AsNonOwningOrganiser_ReturnsForbidden()
    {
        var (_, _, enrolmentId) = await SeedEnrolmentAsync();

        var otherOrganiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(otherOrganiser, "Organiser");

        var response = await otherOrganiser.PostAsJsonAsync(
            $"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "01:45:00", finishPosition = 12 });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
