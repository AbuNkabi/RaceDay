
namespace RaceDay.Contracts
{
    public class CreateEnrolmentRequest
    {
        public int EventId { get; set; }
        public int CategoryId { get; set; }
    }

    public class UpdateEnrolmentStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class EnrolmentDto
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int CategoryId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
