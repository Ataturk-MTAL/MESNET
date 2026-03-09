using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Core.Aggregates;

/// <summary>
/// Öğretmen haftalık ders programı (Event Sourcing Aggregate)
/// Her güncelleme bir event olarak kaydedilir → değişiklik geçmişi takip edilir.
/// "Geçerli program" = son event'ten oluşan güncel state.
/// </summary>
public sealed record TeacherSchedule(
    Guid Id,
    Guid TeacherId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    int AcademicYear,
    AcademicSemester Semester,
    /// <summary>
    /// Marten LINQ sorgularında kullanılan düz alanlar.
    /// SmartEnum JSON'da string olarak serialize edilir ("Spring") ama Marten LINQ
    /// s.Semester.Name/Number ifadesini nested path olarak çevirir ve NULL döner.
    /// Bu property'ler JSON'da primitive olarak kaydedilir, LINQ sorunsuz çalışır.
    /// </summary>
    string SemesterName,
    int SemesterNumber,
    List<DailySchedule> WeeklySchedule,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy,
    string? UpdatedBy,
    int Version)
{
    /// <summary>
    /// İlk oluşturma — ScheduleCreated event'inden aggregate oluşturur
    /// </summary>
    public static TeacherSchedule Create(ScheduleCreated e) => new(
        e.ScheduleId,
        e.TeacherId,
        e.InstitutionId,
        e.AcademicPeriodId,
        e.AcademicYear,
        AcademicSemester.FromName(e.Semester, ignoreCase: true),
        e.Semester,
        AcademicSemester.FromName(e.Semester, ignoreCase: true).Number,
        MapWeeklySchedule(e.WeeklySchedule),
        e.Timestamp,
        null,
        e.UpdatedBy,
        null,
        Version: 1);

    /// <summary>
    /// Program güncelleme — ScheduleUpdated event'ini uygular
    /// </summary>
    public TeacherSchedule Apply(ScheduleUpdated e) => this with
    {
        WeeklySchedule = MapWeeklySchedule(e.WeeklySchedule),
        UpdatedAt = e.Timestamp,
        UpdatedBy = e.UpdatedBy,
        Version = Version + 1
    };

    private static List<DailySchedule> MapWeeklySchedule(List<DailyScheduleData> data)
    {
        return data.Select(d => new DailySchedule
        {
            Day = Enum.Parse<DayOfWeek>(d.Day),
            Periods = d.Periods.Select(p => new PeriodSlot
            {
                PeriodNumber = p.PeriodNumber,
                Status = SlotStatus.FromName(p.Status, ignoreCase: true),
                CourseName = p.CourseName,
                AssignedBusinessId = p.AssignedBusinessId
            }).ToList()
        }).ToList();
    }
}

/// <summary>
/// Günlük ders programı
/// </summary>
public sealed class DailySchedule
{
    public DayOfWeek Day { get; init; }
    public List<PeriodSlot> Periods { get; init; } = new();
}

/// <summary>
/// Bir ders saati slot'u
/// </summary>
public sealed class PeriodSlot
{
    public int PeriodNumber { get; init; }
    public SlotStatus Status { get; init; } = SlotStatus.Free;
    public string? CourseName { get; init; }
    public Guid? AssignedBusinessId { get; set; }
}

// ── Event'lerde kullanılan veri yapıları (serializable, enum-free) ──

/// <summary>
/// Günlük program verisi (event payload — string tabanlı, aggregate bağımsız)
/// </summary>
public sealed record DailyScheduleData(
    string Day,
    List<PeriodSlotData> Periods);

/// <summary>
/// Ders saati verisi (event payload)
/// </summary>
public sealed record PeriodSlotData(
    int PeriodNumber,
    string Status,
    string? CourseName,
    Guid? AssignedBusinessId = null);
