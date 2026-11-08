using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace RaceDayApi.Tests;

public class UploadTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public UploadTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent ImageContent(string contentType = "image/png", string fileName = "image.png")
    {
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
    }

    [Fact]
    public async Task UploadBanner_AsOwningOrganiser_ReturnsOk_AndUrlIsSavedOnTheEvent()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner", ImageContent());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var upload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("fake.blob.local", upload.GetProperty("bannerImageUrl").GetString());

        var ev = await organiser.GetFromJsonAsync<JsonElement>($"/api/events/{eventId}");
        Assert.Contains("fake.blob.local", ev.GetProperty("bannerImageUrl").GetString());
    }

    [Fact]
    public async Task UploadBanner_AsNonOwningOrganiser_ReturnsForbidden()
    {
        var owner = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(owner, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(owner);

        var other = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(other, "Organiser");

        var response = await other.PostAsync($"/api/events/{eventId}/banner", ImageContent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsParticipant_ReturnsForbidden()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.PostAsync($"/api/events/{eventId}/banner", ImageContent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_Unauthenticated_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/events/1/banner", ImageContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WrongFileType_ReturnsBadRequest()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");
        var eventId = await TestHelpers.CreateEventAsync(organiser);

        var response = await organiser.PostAsync(
            $"/api/events/{eventId}/banner",
            ImageContent("text/plain", "notes.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_AsParticipant_ReturnsOk_AndShowsOnProfile()
    {
        var participant = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(participant, "Participant");

        var response = await participant.PutAsync("/api/users/me/profile-picture", ImageContent());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var profile = await participant.GetFromJsonAsync<JsonElement>("/api/users/me");
        Assert.Contains("fake.blob.local", profile.GetProperty("profilePictureUrl").GetString());
    }

    [Fact]
    public async Task UploadProfilePicture_AsOrganiser_ReturnsForbidden()
    {
        var organiser = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(organiser, "Organiser");

        var response = await organiser.PutAsync("/api/users/me/profile-picture", ImageContent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
