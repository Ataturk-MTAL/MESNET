namespace MESNET.Business.Application.Commands;

public sealed record UpdateCapacity(
    Guid BusinessId,
    int TotalSlots,
    int OccupiedSlots);
