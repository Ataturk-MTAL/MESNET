namespace MESNET.Reporting.Core.Models;

/// <summary>
/// Form 7: İşletme Bazlı Aylık Devam-Devamsızlık Bildirim Çizelgesi verileri.
/// MEB standart formu — her işletme için ayrı bir form üretilir.
/// </summary>
public sealed class MonthlyAttendanceReportData
{
    public Guid DocumentId { get; init; } = Guid.NewGuid();

    // İlişkili entity ID'leri (GeneratedDocument'a kopyalanır)
    public Guid? InstitutionId { get; init; }
    public Guid? BusinessId { get; init; }
    public Guid? AcademicPeriodId { get; init; }
    public Guid? TeacherId { get; init; }

    // Üst bilgi alanları
    public required string InstitutionName { get; init; }
    public required string BusinessName { get; init; }
    public string? BusinessPhone { get; init; }
    public string? BusinessEmail { get; init; }
    public required string AcademicYear { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public DateOnly DocumentDate { get; init; }

    // İmza alanları
    public required string BusinessContactName { get; init; }
    public required string CoordinatorTeacherName { get; init; }
    public required string VicePrincipalName { get; init; }

    // Tatil/iş günü bilgisi
    public List<int> WeekendDays { get; init; } = [];
    public List<int> HolidayDays { get; init; } = [];

    // Öğrenci listesi
    public List<StudentAttendanceRow> Students { get; init; } = [];
}

/// <summary>
/// Tek bir öğrencinin aylık devamsızlık satırı.
/// MEB formunda her öğrenci için S (Sabah) ve Ö (Öğleden sonra) olmak üzere 2 satır vardır.
/// </summary>
public sealed class StudentAttendanceRow
{
    public required string ClassName { get; init; }
    public required string StudentNumber { get; init; }
    public required string StudentName { get; init; }
    public required string BranchName { get; init; }

    /// <summary>
    /// Sabah yarım gün devamsızlık işaretleri: gün no → sembol kodu.
    /// null veya key yoksa = devam etti.
    /// Semboller: D, S, SV, İ, SH, TU, C, T, GÖ, F, N, G, DA, YA, SY, ÖY
    /// </summary>
    public Dictionary<int, string?> MorningMarks { get; init; } = new();

    /// <summary>
    /// Öğleden sonra yarım gün devamsızlık işaretleri: gün no → sembol kodu.
    /// </summary>
    public Dictionary<int, string?> AfternoonMarks { get; init; } = new();

    public int ExcusedAbsences { get; init; }
    public int UnexcusedAbsences { get; init; }
}
