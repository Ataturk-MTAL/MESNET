namespace MESNET.Reporting.Core.ReadModels;

/// <summary>
/// Öğrenci-ay bazlı devamsızlık kayıtları — Attendance modülü event'lerinden oluşturulur.
/// Her öğrenci × yıl × ay kombinasyonu için tek bir kayıt tutulur.
/// </summary>
public class StudentAttendanceReportView
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>
    /// Devamsız günler listesi (gün numarası + devamsızlık türü)
    /// </summary>
    public List<AbsentDayEntry> AbsentDays { get; set; } = [];
}

/// <summary>
/// Tek bir devamsızlık gün kaydı
/// </summary>
public sealed record AbsentDayEntry(int Day, string AbsenceType);
