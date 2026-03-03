namespace MESNET.Coordination.Application.Commands;

public sealed record UpdateBusinessAssignedHours(
    Guid BusinessId,
    int AssignedHours,
    Guid InstitutionId,
    string UpdatedBy);
