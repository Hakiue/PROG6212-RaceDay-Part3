using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDayApi.Tests;

public static class TestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Registers a new user and logs them in on the given HttpClient, so the client's
    /// cookie container carries the session for subsequent requests. Returns a unique
    /// email so parallel tests don't collide on the unique-email constraint.
    /// </summary>
    public static async Task<string> RegisterAndLoginAsync(HttpClient client, string role)
    {
        var email = $"{role.ToLower()}.{Guid.NewGuid():N}@raceday.co.za";

        var registerBody = new
        {
            firstName = "Test",
            lastName = role,
            email,
            password = "Password123!",
            role,
            phoneNumber = "0821234567"
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerBody);
        registerResponse.EnsureSuccessStatusCode();

        var loginBody = new { email, password = "Password123!" };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginBody);
        loginResponse.EnsureSuccessStatusCode();

        return email;
    }

    public static async Task<int> CreateEventAsync(HttpClient organiserClient)
    {
        var body = new
        {
            name = "Test Event",
            description = "Created for a unit test",
            eventDate = DateTime.UtcNow.AddMonths(1),
            location = "Cape Town",
            distanceKm = 10.0m,
            eventTypeId = 1
        };

        var response = await organiserClient.PostAsJsonAsync("/api/events", body);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("eventId").GetInt32();
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiserClient, int eventId)
    {
        var body = new { name = "Open 10km", minAge = (int?)null, maxAge = (int?)null, distanceKm = 10.0m };
        var response = await organiserClient.PostAsJsonAsync($"/api/events/{eventId}/categories", body);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("categoryId").GetInt32();
    }
}
