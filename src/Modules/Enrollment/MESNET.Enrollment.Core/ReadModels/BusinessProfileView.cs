using MESNET.Common.Shared;

namespace MESNET.Enrollment.Core.ReadModels;

public class BusinessProfileView
{
    public Guid Id { get; set; }
    public string BusinessName { get; set; } = default!;
    public int TotalSlots { get; set; }
    public int OccupiedSlots { get; set; }
    public int AvailableCapacity => TotalSlots - OccupiedSlots;
    public Location? Location { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
