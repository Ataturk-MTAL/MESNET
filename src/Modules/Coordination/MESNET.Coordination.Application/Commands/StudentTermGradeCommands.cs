namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// İşletmenin öğrenci dönem notlarını girmesi/güncellemesi (taslak). BusinessId ve EnteredByName
/// token'dan endpoint tarafından set edilir — kullanıcı-girişli DEĞİL (güvenlik/kapsam).
/// </summary>
public sealed record EnterStudentTermGrade(
    Guid StudentId,
    Guid AcademicPeriodId,
    List<int> PracticeGrades,
    List<int> ServiceGrades,
    List<int> ProjectGrades,
    List<int> ExperimentGrades,
    string? MasterInstructorName)
{
    public Guid BusinessId { get; init; }
    public string? EnteredByName { get; init; }
}

/// <summary>Girilen taslağı kesin gönderir (Draft → Submitted). BusinessId token'dan (kapsam).</summary>
public sealed record SubmitStudentTermGrade(Guid StudentTermGradeId)
{
    public Guid BusinessId { get; init; }
}
