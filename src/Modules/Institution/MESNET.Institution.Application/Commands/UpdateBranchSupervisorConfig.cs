namespace MESNET.Institution.Application.Commands;

public sealed record UpdateBranchSupervisorConfig(
    Guid InstitutionId,
    string FieldCode,
    int DepartmentHeadCount,
    int WorkshopHeadCount);
