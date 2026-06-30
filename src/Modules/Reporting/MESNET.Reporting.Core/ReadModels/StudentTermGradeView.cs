namespace MESNET.Reporting.Core.ReadModels;

/// <summary>
/// İşletmenin gönderdiği öğrenci dönem notları (Coordination.StudentTermGradeSubmitted'den).
/// Dönem Not Fişi'nin işletme-payı puanlarının kaynağı; ortak alanlar (öğrenci/işletme) için
/// StudentPlacementReportView ile zenginleştirilir.
/// </summary>
public class StudentTermGradeView
{
    public Guid Id { get; set; }            // StudentTermGradeId
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    public List<int> PracticeGrades { get; set; } = [];     // Temrin
    public List<int> ServiceGrades { get; set; } = [];      // İş-Hizmet
    public List<int> ProjectGrades { get; set; } = [];      // Proje
    public List<int> ExperimentGrades { get; set; } = [];   // Deney

    public decimal? TermAverage { get; set; }
    public string? MasterInstructorName { get; set; }
    public DateTime SubmittedAt { get; set; }
}
