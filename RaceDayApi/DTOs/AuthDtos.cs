namespace RaceDayApi.DTOs;

public record RegisterRequest(string FirstName, string LastName, string Email, string Password, string Role, string? PhoneNumber);

public record LoginRequest(string Email, string Password);

public record AuthResponse(int UserId, string FirstName, string LastName, string Email, string Role);
