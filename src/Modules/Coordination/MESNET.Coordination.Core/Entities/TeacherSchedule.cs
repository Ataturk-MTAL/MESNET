using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Core.Entities;

/// <summary>
/// Öğretmen haftalık ders programı (Document storage)
/// </summary>
public sealed class TeacherSchedule
{
    public Guid Id { get; init; }
    public Guid TeacherId { get; init; }
    public Guid InstitutionId { get; init; }
    public Guid AcademicPeriodId { get; init; }

    /// <summary>
    /// Akademik yıl (örn: 2025)
    /// </summary>
    public int AcademicYear { get; init; }

    /// <summary>
    /// Akademik dönem (Güz/Bahar)
    /// </summary>
    public AcademicSemester Semester { get; init; } = AcademicSemester.Fall;

    /// <summary>
    /// Haftalık program (Pazartesi-Cuma, 5 gün)
    /// </summary>
    public List<DailySchedule> WeeklySchedule { get; set; } = new();

    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; init; } = string.Empty;
}

/// <summary>
/// Günlük ders programı
/// </summary>
public sealed class DailySchedule
{
    /// <summary>
    /// Gün (Pazartesi-Cuma)
    /// </summary>
    public DayOfWeek Day { get; init; }

    /// <summary>
    /// O günün ders saatleri
    /// </summary>
    public List<PeriodSlot> Periods { get; init; } = new();
}

/// <summary>
/// Bir ders saati slot'u
/// </summary>
public sealed class PeriodSlot
{
    /// <summary>
    /// Ders numarası (1, 2, 3, ..., N)
    /// </summary>
    public int PeriodNumber { get; init; }

    /// <summary>
    /// Durum (Dolu/Boş)
    /// </summary>
    public SlotStatus Status { get; init; } = SlotStatus.Free;

    /// <summary>
    /// Ders adı (opsiyonel - dolu ise)
    /// </summary>
    public string? CourseName { get; init; }

    /// <summary>
    /// İşletme ataması (boş saat ise işletme atanabilir)
    /// Raporlama ve planlama için kullanılır
    /// </summary>
    public Guid? AssignedBusinessId { get; set; }
}
