namespace RaceDayApi.DTOs;

public record UserProfileDto(int UserId, string FirstName, string LastName, string Email, string Role, string? PhoneNumber, string? ProfilePictureUrl);

public record UpdateProfileRequest(string FirstName, string LastName, string? PhoneNumber);
