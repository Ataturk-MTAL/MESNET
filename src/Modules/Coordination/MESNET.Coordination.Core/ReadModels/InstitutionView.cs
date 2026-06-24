using MESNET.Common.Shared;

namespace MESNET.Coordination.Core.ReadModels;

/// <summary>
/// Institution modülünden InstitutionUpdated event'i ile beslenen read model.
/// Coordination'ın kurum konumu + günlük ders sayısına event-tabanlı erişimini sağlar
/// (modüller arası doğrudan DB/şema erişimi YASAK — bkz. CLAUDE.md).
/// </summary>
public class InstitutionView
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public Location? Location { get; set; }
    public int DailyPeriodCount { get; set; }
}
