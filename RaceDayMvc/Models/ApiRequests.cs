namespace RaceDayMvc.Models;

// Bodies this app sends to the RaceDay API.

public record RegisterRequest(string FirstName, string LastName, string Email, string Password, string Role, string? PhoneNumber);

public record LoginRequest(string Email, string Password);

public record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber);

public record EventRequest(string Name, string? Description, DateTime EventDate, string Location, decimal DistanceKm, int EventTypeId);

public record CategoryRequest(string Name, int? MinAge, int? MaxAge, decimal? DistanceKm);

public record EnrolRequest(int CategoryId);

public record StatusRequest(string Status);

public record ResultRequest(TimeSpan FinishTime, int FinishPosition);
