namespace RaceDayApi.DTOs;

public record ResultCreateRequest(TimeSpan FinishTime, int FinishPosition);

public record ResultDto(
    int ResultId,
    int EnrolmentId,
    string ParticipantName,
    int EventId,
    string EventName,
    DateTime EventDate,
    string CategoryName,
    TimeSpan FinishTime,
    int FinishPosition,
    int TotalFinishers);
