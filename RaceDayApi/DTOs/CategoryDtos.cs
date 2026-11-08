namespace RaceDayApi.DTOs;

public record CategoryCreateRequest(string Name, int? MinAge, int? MaxAge, decimal? DistanceKm);

public record CategoryDto(int CategoryId, int EventId, string Name, int? MinAge, int? MaxAge, decimal? DistanceKm);
