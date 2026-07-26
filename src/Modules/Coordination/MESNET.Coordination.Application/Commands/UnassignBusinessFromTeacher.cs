namespace MESNET.Coordination.Application.Commands;

public sealed record UnassignBusinessFromTeacher(
    Guid BusinessId,
    Guid InstitutionId,
    string UnassignedBy,
    string BranchCode = "",
    Guid AcademicPeriodId = default);
