namespace MESNET.Reporting.Core.ReadModels;

/// <summary>
/// İş takvimi bilgileri — Attendance modülü WorkCalendarUpdated event'inden oluşturulur.
/// Aylık devamsızlık formunda tatil/iş günü hesaplaması için kullanılır.
/// </summary>
public class WorkCalendarReportView
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public int Year { get; set; }
    public List<CalendarDayEntry> RestrictedDays { get; set; } = [];
}

/// <summary>
/// Tatil/izin günü kaydı
/// </summary>
public sealed record CalendarDayEntry(DateTime Date, string Type, string Description);
