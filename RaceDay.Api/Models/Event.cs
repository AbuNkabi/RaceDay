
namespace RaceDay.Api.Models
{
    public class Event
    {
        public int EventId { get; set; }

        public int OrganiserId { get; set; }

        public string EventName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime EventDate { get; set; }

        public string Location { get; set; } = string.Empty;

        // Private Azure blob name (null when no image uploaded)
        public string? ImageUrl { get; set; }

        public User? Organiser { get; set; }

        public ICollection<EventCategory> EventCategories { get; set; }
            = new List<EventCategory>();
    }
}
