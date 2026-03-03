namespace MESNET.Coordination.Application.Commands;

public sealed record UnassignBusinessSlot(
    Guid BusinessId,
    string Day,
    int PeriodNumber,
    Guid InstitutionId,
    string UnassignedBy);
