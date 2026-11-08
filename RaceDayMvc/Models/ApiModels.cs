namespace RaceDayMvc.Models;

// Shapes of the JSON the RaceDay API returns (property names match the API's camelCase output).

public record AuthResponse(int UserId, string FirstName, string LastName, string Email, string Role);

public record UserProfile(int UserId, string FirstName, string LastName, string Email, string Role, string? PhoneNumber, string? ProfilePictureUrl);

public record EventSummary(
    int EventId,
    int OrganiserId,
    string Name,
    string? Description,
    DateTime EventDate,
    string Location,
    decimal DistanceKm,
    int EventTypeId,
    string EventTypeName,
    string? BannerImageUrl,
    int EnrolmentCount);

public record EventTypeItem(int EventTypeId, string TypeName);

public record CategoryItem(int CategoryId, int EventId, string Name, int? MinAge, int? MaxAge, decimal? DistanceKm);

public record EnrolmentItem(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    string ParticipantEmail,
    int EventId,
    string EventName,
    DateTime EventDate,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string Status);

public record ResultItem(
    int ResultId,
    int EnrolmentId,
    string ParticipantName,
    int EventId,
    string EventName,
    DateTime EventDate,
    string CategoryName,
    TimeSpan FinishTime,
    int FinishPosition,
    int TotalFinishers);

public record BannerResponse(string BannerImageUrl);

public record ProfilePictureResponse(string ProfilePictureUrl);
