namespace RaceDayApi.Models;

public class Result
{
    public int ResultId { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }

    public int RecordedByOrganiserId { get; set; }
    public User? RecordedByOrganiser { get; set; }

    public TimeSpan FinishTime { get; set; }
    public int FinishPosition { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
