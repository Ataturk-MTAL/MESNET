namespace MESNET.Coordination.Core.Entities;

/// <summary>
/// Tekil ziyaret kaydı = 1 günlük form.
/// Her kayıt benzersiz Id'ye sahiptir — QR kod kaynağı olarak kullanılır.
/// </summary>
public sealed class WeeklyVisitAssignment
{
    /// <summary>QR kod kaynağı olan benzersiz ID</summary>
    public Guid Id { get; set; }

    /// <summary>Bu kaydın ait olduğu haftalık plan</summary>
    public Guid PlanId { get; set; }

    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;

    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;

    public string BranchCode { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;

    /// <summary>Gerçek ziyaret tarihi (ör. 2026-03-18)</summary>
    public DateOnly VisitDate { get; set; }

    /// <summary>Gün adı: "Monday", "Tuesday" vb.</summary>
    public string Day { get; set; } = string.Empty;

    /// <summary>O gün kaç ders saati koordinasyon yapılıyor</summary>
    public int PeriodCount { get; set; }

    /// <summary>ISO hafta numarası</summary>
    public int WeekNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
