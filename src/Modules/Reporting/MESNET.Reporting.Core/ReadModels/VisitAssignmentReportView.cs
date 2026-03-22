namespace MESNET.Reporting.Core.ReadModels;

/// <summary>
/// Haftalık ziyaret ataması — Coordination modülünün WeeklyVisitsGenerated event'inden oluşturulur.
/// Form 3 (Günlük Rehberlik Formu) toplu üretiminde kullanılır.
/// Öğretmenin hangi gün hangi işletmeye gittiği bilgisini saklar.
/// </summary>
public class VisitAssignmentReportView
{
    public Guid Id { get; set; } // AssignmentId
    public Guid TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public string BranchCode { get; set; } = "";
    public string BranchName { get; set; } = "";
    public DateOnly VisitDate { get; set; }
    public int StudentCount { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
}
