namespace RaceDayApi.DTOs;

public record EnrolmentCreateRequest(int CategoryId);

public record EnrolmentStatusUpdateRequest(string Status);

public record EnrolmentDto(
    int EnrolmentId,
    int ParticipantId,
    string ParticipantName,
    string ParticipantEmail,
    int EventId,
    string EventName,
    DateTime EventDate,
    int CategoryId,
    string CategoryName,
    DateTime EnrolmentDate,
    string Status);
