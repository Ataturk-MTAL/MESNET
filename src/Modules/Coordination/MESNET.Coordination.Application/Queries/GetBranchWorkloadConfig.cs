namespace MESNET.Coordination.Application.Queries;

public sealed record GetBranchWorkloadConfig(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode);
