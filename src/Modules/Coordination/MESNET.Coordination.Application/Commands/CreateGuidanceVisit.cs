using MESNET.Coordination.Core.ValueObjects;

namespace MESNET.Coordination.Application.Commands;

public sealed record CreateGuidanceVisit(
    Guid TeacherId,
    Guid BusinessId,
    Guid InstitutionId,
    DateTime VisitDate,
    List<StudentVisitNote> StudentNotes,
    string? InstructorMeetingNotes,
    string? IssuesIdentified,
    string? ActionsTaken,
    string? GeneralAssessment);

public sealed record UpdateGuidanceVisit(
    Guid VisitId,
    DateTime VisitDate,
    List<StudentVisitNote> StudentNotes,
    string? InstructorMeetingNotes,
    string? IssuesIdentified,
    string? ActionsTaken,
    string? GeneralAssessment);

public sealed record SubmitGuidanceVisit(Guid VisitId);

public sealed record ApproveGuidanceVisit(Guid VisitId);
