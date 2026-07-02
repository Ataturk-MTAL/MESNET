namespace MESNET.Coordination.Core.ReadModels;

public class AcademicPeriodView
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime? ClosedAt { get; set; }

    // Dönem sonu not giriş penceresi (müdür/müdür yardımcısı belirler — Institution.GradeEntryWindowSet)
    public DateOnly? GradeEntryStartDate { get; set; }
    public DateOnly? GradeEntryEndDate { get; set; }

    /// <summary>Verilen tarihte not girişi açık mı (dönem aktif + tarih pencere içinde).</summary>
    public bool IsGradeEntryOpen(DateOnly today) =>
        IsActive
        && GradeEntryStartDate is { } start && GradeEntryEndDate is { } end
        && today >= start && today <= end;
}
