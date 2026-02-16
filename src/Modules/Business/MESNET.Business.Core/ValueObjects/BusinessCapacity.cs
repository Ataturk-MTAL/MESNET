namespace MESNET.Business.Core.ValueObjects;

public sealed record BusinessCapacity
{
    public int TotalSlots { get; init; }
    public int OccupiedSlots { get; init; }
    public int AvailableSlots => TotalSlots - OccupiedSlots;
    public bool IsFull => AvailableSlots <= 0;
}
