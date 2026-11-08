using System.ComponentModel.DataAnnotations;
using RaceDayMvc.Models;

namespace RaceDayMvc.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your first name.")]
    [StringLength(50, ErrorMessage = "First name can be at most 50 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your last name.")]
    [StringLength(50, ErrorMessage = "Last name can be at most 50 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "That doesn't look like a valid email address.")]
    [StringLength(100, ErrorMessage = "Email can be at most 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That doesn't look like a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number can be at most 20 characters.")]
    [Display(Name = "Phone number (optional)")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type your password again.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords don't match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose how you'll use RaceDay.")]
    [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Choose Participant or Organiser.")]
    public string Role { get; set; } = "Participant";
}

public class EventListViewModel
{
    public List<EventSummary> Events { get; set; } = new();
    public List<EventTypeItem> EventTypes { get; set; } = new();
    public string? EventType { get; set; }
    public string? Location { get; set; }
    public bool UpcomingOnly { get; set; } = true;
    public string? LoadError { get; set; }
}

public class EventDetailsViewModel
{
    public EventSummary Event { get; set; } = null!;
    public List<CategoryItem> Categories { get; set; } = new();
    public EnrolmentItem? ExistingEnrolment { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Enter your first name.")]
    [StringLength(50, ErrorMessage = "First name can be at most 50 characters.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your last name.")]
    [StringLength(50, ErrorMessage = "Last name can be at most 50 characters.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That doesn't look like a valid phone number.")]
    [StringLength(20, ErrorMessage = "Phone number can be at most 20 characters.")]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    // Display-only: not posted back
    public string? Email { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public class ErrorViewModel
{
    public int Code { get; set; } = 500;
    public string Title { get; set; } = "Something went wrong";
    public string Message { get; set; } = "Something went wrong on our side. Please try again.";
}
