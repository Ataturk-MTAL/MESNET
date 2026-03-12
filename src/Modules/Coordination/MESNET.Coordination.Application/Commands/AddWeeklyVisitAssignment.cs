namespace MESNET.Coordination.Application.Commands;

public sealed record AddWeeklyVisitAssignment(
    Guid PlanId,
    Guid InstitutionId,
    Guid TeacherId,
    string TeacherName,
    Guid BusinessId,
    string BusinessName,
    string BranchCode,
    string BranchName,
    string Day,
    int PeriodCount,
    string AddedBy);
