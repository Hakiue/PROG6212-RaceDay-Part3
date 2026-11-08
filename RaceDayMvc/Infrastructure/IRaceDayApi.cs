using RaceDayMvc.Models;

namespace RaceDayMvc.Infrastructure;

/// <summary>Every call this app makes to the RaceDay API. The MVC app has no database access of its own.</summary>
public interface IRaceDayApi
{
    // Authentication
    Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request);
    Task<ApiResult> LogoutAsync();

    // Profile
    Task<ApiResult<UserProfile>> GetMeAsync();
    Task<ApiResult<UserProfile>> UpdateMeAsync(UpdateProfileRequest request);
    Task<ApiResult<ProfilePictureResponse>> UploadProfilePictureAsync(IFormFile file);

    // Events
    Task<ApiResult<List<EventSummary>>> GetEventsAsync(string? eventType, string? location, bool upcomingOnly);
    Task<ApiResult<List<EventSummary>>> GetMyEventsAsync();
    Task<ApiResult<EventSummary>> GetEventAsync(int id);
    Task<ApiResult<EventSummary>> CreateEventAsync(EventRequest request);
    Task<ApiResult<EventSummary>> UpdateEventAsync(int id, EventRequest request);
    Task<ApiResult> DeleteEventAsync(int id);
    Task<ApiResult<BannerResponse>> UploadBannerAsync(int eventId, IFormFile file);
    Task<ApiResult<List<EventTypeItem>>> GetEventTypesAsync();

    // Categories
    Task<ApiResult<List<CategoryItem>>> GetCategoriesAsync(int eventId);
    Task<ApiResult<CategoryItem>> CreateCategoryAsync(int eventId, CategoryRequest request);
    Task<ApiResult<CategoryItem>> UpdateCategoryAsync(int id, CategoryRequest request);
    Task<ApiResult> DeleteCategoryAsync(int id);

    // Enrolments
    Task<ApiResult<EnrolmentItem>> EnrolAsync(int eventId, int categoryId);
    Task<ApiResult<List<EnrolmentItem>>> GetMyEnrolmentsAsync();
    Task<ApiResult<List<EnrolmentItem>>> GetEventEnrolmentsAsync(int eventId);
    Task<ApiResult<EnrolmentItem>> UpdateEnrolmentStatusAsync(int enrolmentId, string status);

    // Results
    Task<ApiResult<ResultItem>> CaptureResultAsync(int enrolmentId, ResultRequest request);
    Task<ApiResult<ResultItem>> UpdateResultAsync(int resultId, ResultRequest request);
    Task<ApiResult<List<ResultItem>>> GetMyResultsAsync();
    Task<ApiResult<List<ResultItem>>> GetEventResultsAsync(int eventId);
}
