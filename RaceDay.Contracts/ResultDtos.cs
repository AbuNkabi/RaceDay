
namespace RaceDay.Contracts
{
    public class CreateResultRequest
    {
        public int EnrolmentId { get; set; }
        public int FinishingPosition { get; set; }
        public TimeSpan FinishingTime { get; set; }
    }

    public class ResultDto
    {
        public int Id { get; set; }
        public int EnrolmentId { get; set; }
        public int FinishingPosition { get; set; }
        public TimeSpan FinishingTime { get; set; }
    }
}
