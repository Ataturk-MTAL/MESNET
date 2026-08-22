namespace MESNET.Reporting.Core.ReadModels;

/// <summary>
/// Öğrenci yerleştirme bilgileri — Enrollment ve Business modülü event'lerinden oluşturulur.
/// Aylık devamsızlık formunda öğrenci, işletme ve alan bilgisi için kullanılır.
/// </summary>
public class StudentPlacementReportView
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = "";
    public string StudentNumber { get; set; } = "";
    public string ClassName { get; set; } = "";
    public int ClassYear { get; set; }
    /// <summary>İşletme — okulda stajda null (#159); raporda "Okulda" olarak gösterilir.</summary>
    public Guid? BusinessId { get; set; }
    public string BusinessName { get; set; } = "";
    public string? BusinessPhone { get; set; }
    public string? BusinessEmail { get; set; }
    public string? BusinessContactName { get; set; }
    public string BranchCode { get; set; } = "";
    public string BranchName { get; set; } = "";
    public Guid? TeacherId { get; set; }
    public string TeacherName { get; set; } = "";
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
}
