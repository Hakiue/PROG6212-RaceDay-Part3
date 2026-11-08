using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDayMvc.Models;

namespace RaceDayMvc.Infrastructure;

public class RaceDayApiClient : IRaceDayApi
{
    private const string UnavailableMessage = "We can't reach the RaceDay service right now. Please try again in a moment.";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public RaceDayApiClient(HttpClient http) => _http = http;

    // ---------- Authentication ----------
    public Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/register", request);

    public Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request) =>
        SendAsync<AuthResponse>(HttpMethod.Post, "api/auth/login", request);

    public Task<ApiResult> LogoutAsync() => SendAsync(HttpMethod.Post, "api/auth/logout");

    // ---------- Profile ----------
    public Task<ApiResult<UserProfile>> GetMeAsync() =>
        SendAsync<UserProfile>(HttpMethod.Get, "api/users/me");

    public Task<ApiResult<UserProfile>> UpdateMeAsync(UpdateProfileRequest request) =>
        SendAsync<UserProfile>(HttpMethod.Put, "api/users/me", request);

    public Task<ApiResult<ProfilePictureResponse>> UploadProfilePictureAsync(IFormFile file) =>
        UploadAsync<ProfilePictureResponse>(HttpMethod.Put, "api/users/me/profile-picture", file);

    // ---------- Events ----------
    public Task<ApiResult<List<EventSummary>>> GetEventsAsync(string? eventType, string? location, bool upcomingOnly)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(eventType)) parts.Add($"eventType={Uri.EscapeDataString(eventType)}");
        if (!string.IsNullOrWhiteSpace(location)) parts.Add($"location={Uri.EscapeDataString(location)}");
        if (upcomingOnly) parts.Add("upcomingOnly=true");

        var url = parts.Count == 0 ? "api/events" : "api/events?" + string.Join("&", parts);
        return SendAsync<List<EventSummary>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<List<EventSummary>>> GetMyEventsAsync() =>
        SendAsync<List<EventSummary>>(HttpMethod.Get, "api/events/mine");

    public Task<ApiResult<EventSummary>> GetEventAsync(int id) =>
        SendAsync<EventSummary>(HttpMethod.Get, $"api/events/{id}");

    public Task<ApiResult<EventSummary>> CreateEventAsync(EventRequest request) =>
        SendAsync<EventSummary>(HttpMethod.Post, "api/events", request);

    public Task<ApiResult<EventSummary>> UpdateEventAsync(int id, EventRequest request) =>
        SendAsync<EventSummary>(HttpMethod.Put, $"api/events/{id}", request);

    public Task<ApiResult> DeleteEventAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/events/{id}");

    public Task<ApiResult<BannerResponse>> UploadBannerAsync(int eventId, IFormFile file) =>
        UploadAsync<BannerResponse>(HttpMethod.Post, $"api/events/{eventId}/banner", file);

    public Task<ApiResult<List<EventTypeItem>>> GetEventTypesAsync() =>
        SendAsync<List<EventTypeItem>>(HttpMethod.Get, "api/eventtypes");

    // ---------- Categories ----------
    public Task<ApiResult<List<CategoryItem>>> GetCategoriesAsync(int eventId) =>
        SendAsync<List<CategoryItem>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<ApiResult<CategoryItem>> CreateCategoryAsync(int eventId, CategoryRequest request) =>
        SendAsync<CategoryItem>(HttpMethod.Post, $"api/events/{eventId}/categories", request);

    public Task<ApiResult<CategoryItem>> UpdateCategoryAsync(int id, CategoryRequest request) =>
        SendAsync<CategoryItem>(HttpMethod.Put, $"api/categories/{id}", request);

    public Task<ApiResult> DeleteCategoryAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/categories/{id}");

    // ---------- Enrolments ----------
    public Task<ApiResult<EnrolmentItem>> EnrolAsync(int eventId, int categoryId) =>
        SendAsync<EnrolmentItem>(HttpMethod.Post, $"api/events/{eventId}/enrolments", new EnrolRequest(categoryId));

    public Task<ApiResult<List<EnrolmentItem>>> GetMyEnrolmentsAsync() =>
        SendAsync<List<EnrolmentItem>>(HttpMethod.Get, "api/users/me/enrolments");

    public Task<ApiResult<List<EnrolmentItem>>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EnrolmentItem>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public Task<ApiResult<EnrolmentItem>> UpdateEnrolmentStatusAsync(int enrolmentId, string status) =>
        SendAsync<EnrolmentItem>(HttpMethod.Put, $"api/enrolments/{enrolmentId}/status", new StatusRequest(status));

    // ---------- Results ----------
    public Task<ApiResult<ResultItem>> CaptureResultAsync(int enrolmentId, ResultRequest request) =>
        SendAsync<ResultItem>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/result", request);

    public Task<ApiResult<ResultItem>> UpdateResultAsync(int resultId, ResultRequest request) =>
        SendAsync<ResultItem>(HttpMethod.Put, $"api/results/{resultId}", request);

    public Task<ApiResult<List<ResultItem>>> GetMyResultsAsync() =>
        SendAsync<List<ResultItem>>(HttpMethod.Get, "api/users/me/results");

    public Task<ApiResult<List<ResultItem>>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<ResultItem>>(HttpMethod.Get, $"api/events/{eventId}/results");

    // ---------- Plumbing ----------
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null, HttpContent? content = null)
    {
        try
        {
            using var response = await SendRawAsync(method, url, body, content);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.Failed((int)response.StatusCode, await ReadErrorAsync(response));
            }

            var data = await response.Content.ReadFromJsonAsync<T>(Json);
            return data is null
                ? ApiResult<T>.Failed(502, "The server sent back an empty response.")
                : ApiResult<T>.WithData(data, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            return ApiResult<T>.Failed(503, UnavailableMessage);
        }
    }

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body = null)
    {
        try
        {
            using var response = await SendRawAsync(method, url, body, null);

            return response.IsSuccessStatusCode
                ? ApiResult.Success((int)response.StatusCode)
                : ApiResult.Failure((int)response.StatusCode, await ReadErrorAsync(response));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return ApiResult.Failure(503, UnavailableMessage);
        }
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, object? body, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url);

        if (content is not null)
        {
            request.Content = content;
        }
        else if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        return await _http.SendAsync(request);
    }

    private async Task<ApiResult<T>> UploadAsync<T>(HttpMethod method, string url, IFormFile file)
    {
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();

        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);

        return await SendAsync<T>(method, url, content: form);
    }

    /// <summary>Pulls a human-readable message out of an API error response.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                        return message.GetString()!;

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var first = errors.EnumerateObject()
                            .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                            .SelectMany(p => p.Value.EnumerateArray())
                            .Select(e => e.GetString())
                            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
                        if (first is not null) return first;
                    }

                    if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                        return title.GetString()!;
                }
            }
        }
        catch (JsonException)
        {
            // fall through to the default message
        }

        return (int)response.StatusCode switch
        {
            401 => "Your session has expired. Please log in again.",
            403 => "You don't have permission to do that.",
            404 => "We couldn't find that.",
            409 => "That clashes with existing data.",
            503 => "Image storage isn't set up on the server yet.",
            _ => "Something went wrong. Please try again."
        };
    }
}
