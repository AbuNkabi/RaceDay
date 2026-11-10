namespace RaceDay.Web.Models;

// These mirror the JSON returned by the RaceDay API (matching is case-insensitive).

public class EventModel
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class EnrolmentModel
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public int? CategoryId { get; set; }
    public string Status { get; set; } = "Pending";
}

public class EventEnrolmentModel
{
    public int EnrolmentId { get; set; }
    public int UserId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string Status { get; set; } = "Pending";
}

public class EventResultModel
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public decimal Time { get; set; }
    public int Position { get; set; }
    public int? CategoryId { get; set; }

    public string FormattedTime =>
        TimeSpan.FromSeconds((double)Time).ToString(@"hh\:mm\:ss\.fff");
}

public class ProfileModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
}

// Bodies sent to the API
public class EventRequest
{
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class CreateEnrolmentBody
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }
}

public class ResultBody
{
    public int EnrolmentId { get; set; }
    public decimal Time { get; set; }
    public int Position { get; set; }
}

public class CategoryModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public int MaximumParticipants { get; set; }
}
