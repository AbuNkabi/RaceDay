using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required, Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string Role { get; set; } = "Participant";
    public string? OrganiserCode { get; set; }
}

public class ProfileViewModel
{
    [Required, Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required, Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required, DataType(DataType.DateTime), Display(Name = "Event date")]
    public DateTime EventDate { get; set; } = DateTime.Today.AddDays(7);

    [Required]
    public string Location { get; set; } = string.Empty;

    public string? ExistingImageUrl { get; set; }

    [Display(Name = "Event image")]
    public IFormFile? Image { get; set; }
}

public class EventDetailsViewModel
{
    public EventModel Event { get; set; } = new();
    public bool IsEnrolled { get; set; }
    public int? EnrolmentId { get; set; }
    public List<EventResultModel> Results { get; set; } = new();
    public List<CategoryModel> Categories { get; set; } = new();
}

public class MyEnrolmentViewModel
{
    public EnrolmentModel Enrolment { get; set; } = new();
    public EventModel? Event { get; set; }
}

public class ManageResultsViewModel
{
    public EventModel Event { get; set; } = new();
    public List<EventEnrolmentModel> Enrolments { get; set; } = new();
    public List<EventResultModel> Results { get; set; } = new();
    public List<CategoryModel> Categories { get; set; } = new();

    [Required, Display(Name = "Participant")]
    public int EnrolmentId { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Position { get; set; } = 1;

    [Required, Range(0.001, 1000000, ErrorMessage = "Enter the finishing time in seconds.")]
    [Display(Name = "Finishing time (seconds)")]
    public decimal Time { get; set; }
}

public class ErrorViewModel
{
    public string? Message { get; set; }
}

// Participant view of race results. Data is filtered to the logged-in participant's enrolment IDs.
public class MyRaceResultViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "-";
    public int Position { get; set; }
    public decimal Time { get; set; }

    public string FormattedTime =>
        TimeSpan.FromSeconds((double)Time).ToString(@"hh\:mm\:ss\.fff");
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public List<CategoryModel> Categories { get; set; } = new();
    [Required] public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [Range(0, 100000)] public decimal EntryFee { get; set; }
    [Range(1, 1000000)] public int MaximumParticipants { get; set; } = 100;
}

public class OrganiserDashboardViewModel
{
    public int EventCount { get; set; }
    public int EnrolmentCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int ResultCount { get; set; }
    public List<EventModel> Events { get; set; } = new();
}
