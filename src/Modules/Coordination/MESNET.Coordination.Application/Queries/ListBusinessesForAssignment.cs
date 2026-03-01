namespace MESNET.Coordination.Application.Queries;

public sealed record ListBusinessesForAssignment(
    Guid InstitutionId,
    string? BranchCode = null,
    Guid? TeacherId = null,
    bool? AssignedOnly = null,
    int Page = 1,
    int PageSize = 100);
