namespace MESNET.Coordination.Shared.Events;

/// <summary>
/// İşletme öğrenci dönem notlarını kesin gönderdi. Reporting bu olayı dinleyip Dönem Not Fişi'ni
/// gerçek notlardan üretmek üzere kendi read-model'ini (StudentTermGradeView) oluşturur.
/// </summary>
public sealed record StudentTermGradeSubmitted(
    Guid StudentTermGradeId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    List<int> PracticeGrades,
    List<int> ServiceGrades,
    List<int> ProjectGrades,
    List<int> ExperimentGrades,
    decimal? TermAverage,
    string? MasterInstructorName,
    DateTime SubmittedAt);
