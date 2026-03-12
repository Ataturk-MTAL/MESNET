namespace MESNET.Coordination.Core.Entities;

/// <summary>
/// Haftalık ziyaret planı — belirli bir hafta ve kapsam için oluşturulan
/// ziyaret atamalarının üst grubu.
/// </summary>
public sealed class WeeklyVisitPlan
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    /// <summary>ISO yıl</summary>
    public int Year { get; set; }

    /// <summary>ISO hafta numarası (1-53)</summary>
    public int WeekNumber { get; set; }

    /// <summary>Haftanın pazartesi günü</summary>
    public DateOnly WeekStartDate { get; set; }

    /// <summary>Haftanın cuma günü</summary>
    public DateOnly WeekEndDate { get; set; }

    /// <summary>Kapsam: "Teacher", "Branch" veya "All"</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Scope=Teacher ise ilgili öğretmen</summary>
    public Guid? ScopeTeacherId { get; set; }

    /// <summary>Scope=Branch ise ilgili alan kodu</summary>
    public string? ScopeBranchCode { get; set; }

    /// <summary>Oluşturulan ziyaret sayısı</summary>
    public int AssignmentCount { get; set; }

    public string GeneratedBy { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
