using System.Net;
using System.Net.Http.Json;
using RaceDayApi.Tests;
using Xunit;

namespace RaceDayApi.Tests;

public class EventsTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public EventsTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private object BuildEventBody() => new
    {
        name = "Cape Town City Run",
        description = "A scenic road run through the city bowl.",
        eventDate = DateTime.UtcNow.AddMonths(2),
        location = "Cape Town, Western Cape",
        distanceKm = 21.1m,
        eventTypeId = 1
    };

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");

        var response = await client.PostAsJsonAsync("/api/events", BuildEventBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Participant");

        var response = await client.PostAsJsonAsync("/api/events", BuildEventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/events", BuildEventBody());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetEvents_Public_ReturnsOkWithoutLogin()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetEventById_NonExistent_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsNonOwningOrganiser_ReturnsForbidden()
    {
        var ownerClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(ownerClient, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(ownerClient);

        var otherClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(otherClient, "Organiser");

        var response = await otherClient.PutAsJsonAsync($"/api/events/{eventId}", BuildEventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsOwningOrganiser_ReturnsNoContent()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(client);

        var response = await client.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetMyEvents_OnlyReturnsOwnEvents()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Organiser");
        await TestHelpers.CreateEventAsync(client);

        var otherClient = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(otherClient, "Organiser");
        await TestHelpers.CreateEventAsync(otherClient);

        var response = await client.GetAsync("/api/events/mine");
        var events = await response.Content.ReadFromJsonAsync<List<System.Text.Json.JsonElement>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Single(events!);
    }
}
