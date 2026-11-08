using System.Net;
using System.Net.Http.Json;
using RaceDayApi.Tests;
using Xunit;

namespace RaceDayApi.Tests;

public class AuthTests : IClassFixture<RaceDayWebApplicationFactory>
{
    private readonly RaceDayWebApplicationFactory _factory;

    public AuthTests(RaceDayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ValidOrganiser_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var body = new
        {
            firstName = "Thandi",
            lastName = "Nkosi",
            email = $"organiser.{Guid.NewGuid():N}@raceday.co.za",
            password = "Password123!",
            role = "Organiser",
            phoneNumber = "0821234567"
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var body = new
        {
            firstName = "Bad",
            lastName = "Role",
            email = $"badrole.{Guid.NewGuid():N}@raceday.co.za",
            password = "Password123!",
            role = "Administrator",
            phoneNumber = (string?)null
        };

        var response = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = $"dupe.{Guid.NewGuid():N}@raceday.co.za";

        var body = new
        {
            firstName = "First",
            lastName = "User",
            email,
            password = "Password123!",
            role = "Participant",
            phoneNumber = (string?)null
        };

        var first = await client.PostAsJsonAsync("/api/auth/register", body);
        var second = await client.PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var email = await TestHelpers.RegisterAndLoginAsync(client, "Organiser");

        // RegisterAndLoginAsync already logged in once; log in again explicitly to assert the response shape
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Password123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"wrongpw.{Guid.NewGuid():N}@raceday.co.za";

        await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Wrong",
            lastName = "Password",
            email,
            password = "Password123!",
            role = "Participant",
            phoneNumber = (string?)null
        });

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "IncorrectPassword!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonexistentEmail_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "doesnotexist@raceday.co.za",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ThenProtectedEndpoint_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        await TestHelpers.RegisterAndLoginAsync(client, "Participant");

        var logoutResponse = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var afterLogout = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
