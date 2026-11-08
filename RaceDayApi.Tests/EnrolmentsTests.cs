using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayApi.Tests;
using Xunit;

namespace RaceDayApi.Tests;

public class EnrolmentsTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EnrolmentsTests(RaceDayWebApplicationFactory factory)
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

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreated_AndIsRecordedCorrectly()
    {
        var (_, eventId, categoryId) = await SeedEventWithCategoryAsync();

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var myEnrolments = await participant.GetFromJsonAsync<List<JsonElement>>("/api/users/me/enrolments");
        Assert.Single(myEnrolments!);
        Assert.Equal(eventId, myEnrolments![0].GetProperty("eventId").GetInt32());
        Assert.Equal(categoryId, myEnrolments[0].GetProperty("categoryId").GetInt32());
    }

    [Fact]
    public async Task Enrol_Twice_ReturnsConflict()
    {
        var (_, eventId, categoryId) = await SeedEventWithCategoryAsync();

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");

        var first = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var second = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        var (organiser, eventId, categoryId) = await SeedEventWithCategoryAsync();

        // The event's own organiser tries to enrol as if they were a participant
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEnrolmentsForEvent_AsNonOwningOrganiser_ReturnsForbidden()
    {
        var (_, eventId, categoryId) = await SeedEventWithCategoryAsync();

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");
        await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        var otherOrganiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(otherOrganiser, "Organiser");

        var response = await otherOrganiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEnrolmentStatus_AsParticipant_ReturnsForbidden()
    {
        var (organiser, eventId, categoryId) = await SeedEventWithCategoryAsync();

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");
        var enrolResponse = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<JsonElement>();
        var enrolmentId = enrolment.GetProperty("enrolmentId").GetInt32();

        var response = await participant.PutAsJsonAsync($"/api/enrolments/{enrolmentId}/status", new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
