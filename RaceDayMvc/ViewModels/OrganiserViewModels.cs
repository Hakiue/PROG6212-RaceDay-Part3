using System.ComponentModel.DataAnnotations;
using RaceDayMvc.Models;

namespace RaceDayMvc.ViewModels;

// ---------- Dashboard ----------

public class DashboardViewModel
{
    public List<EventSummary> Events { get; set; } = new();

    public int TotalEvents => Events.Count;
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);

    public List<EventSummary> Upcoming =>
        Events.Where(e => e.EventDate >= DateTime.Now).OrderBy(e => e.EventDate).ToList();

    public List<EventSummary> Past =>
        Events.Where(e => e.EventDate < DateTime.Now).OrderByDescending(e => e.EventDate).ToList();

    public EventSummary? NextEvent => Upcoming.FirstOrDefault();
}

// ---------- Events management ----------

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Give the event a name.")]
    [StringLength(100, ErrorMessage = "The name can be at most 100 characters.")]
    [Display(Name = "Event name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [StringLength(4000, ErrorMessage = "The description is too long (4000 characters at most).")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Choose the date and start time.")]
    [Display(Name = "Date and start time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Enter the location.")]
    [StringLength(150, ErrorMessage = "The location can be at most 150 characters.")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the distance in kilometres.")]
    [Range(0.1, 9999, ErrorMessage = "The distance must be more than 0 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Choose an event type.")]
    [Display(Name = "Event type")]
    public int EventTypeId { get; set; }

    // Display-only (not posted back)
    public string? BannerImageUrl { get; set; }
    public List<EventTypeItem> EventTypes { get; set; } = new();

    public bool IsEdit => EventId.HasValue;
}

// ---------- Categories ----------

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Give the category a name.")]
    [StringLength(50, ErrorMessage = "The name can be at most 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 120, ErrorMessage = "Minimum age must be between 0 and 120.")]
    [Display(Name = "Minimum age")]
    public int? MinAge { get; set; }

    [Range(0, 120, ErrorMessage = "Maximum age must be between 0 and 120.")]
    [Display(Name = "Maximum age")]
    public int? MaxAge { get; set; }

    [Range(0.1, 9999, ErrorMessage = "The distance must be more than 0 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
}

public class CategoriesViewModel
{
    public EventSummary Event { get; set; } = null!;
    public List<CategoryItem> Categories { get; set; } = new();
    public CategoryFormViewModel Form { get; set; } = new();
}

// ---------- Enrolments ----------

public class EventEnrolmentsViewModel
{
    public EventSummary Event { get; set; } = null!;
    public List<EnrolmentItem> Enrolments { get; set; } = new();

    public int Count(string status) =>
        Enrolments.Count(e => string.Equals(e.Status, status, StringComparison.OrdinalIgnoreCase));
}

// ---------- Results ----------

public class ResultRowViewModel
{
    public EnrolmentItem Enrolment { get; set; } = null!;
    public ResultItem? Result { get; set; }
}

public class EventResultsViewModel
{
    public EventSummary Event { get; set; } = null!;
    public List<ResultRowViewModel> Rows { get; set; } = new();

    /// <summary>Position suggested for the next result that is captured.</summary>
    public int NextPosition { get; set; } = 1;

    public bool HasHappened => Event.EventDate <= DateTime.Now;
}
