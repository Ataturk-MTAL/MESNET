namespace MESNET.Business.Shared.Events;

public sealed record BusinessCapacityChanged(
    Guid BusinessId,
    int TotalSlots,
    int OccupiedSlots);
