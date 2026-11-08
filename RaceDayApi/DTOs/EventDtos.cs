namespace RaceDayApi.DTOs;

public record EventCreateRequest(string Name, string? Description, DateTime EventDate, string Location, decimal DistanceKm, int EventTypeId);

public record EventUpdateRequest(string Name, string? Description, DateTime EventDate, string Location, decimal DistanceKm, int EventTypeId);

public record EventDto(
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

public record EventTypeDto(int EventTypeId, string TypeName);
