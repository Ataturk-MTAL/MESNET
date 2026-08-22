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

/// <summary>
/// <b>Okulda staj</b> yapan öğrencinin dönem notunu girer/günceller (taslak) — #171.
///
/// <para>İşletme akışının aynısı değildir: <c>BusinessId</c> yoktur ve olamaz (işverensiz
/// yerleştirme, #159), kapsam <c>business_id</c> claim'i yerine <b>kurum</b> ve öğrencinin
/// okulda staj yerleştirmesi üzerinden kurulur. <c>MasterInstructorName</c> de yoktur —
/// usta öğretici işletme tarafının kavramıdır.</para>
/// </summary>
public sealed record EnterSchoolTermGrade(
    Guid StudentId,
    Guid AcademicPeriodId,
    List<int> PracticeGrades,
    List<int> ServiceGrades,
    List<int> ProjectGrades,
    List<int> ExperimentGrades)
{
    /// <summary>Token'daki <c>institution_id</c> claim'i — uçta doldurulur, istekten ALINMAZ.</summary>
    public Guid InstitutionId { get; init; }

    public string? EnteredByName { get; init; }
}

/// <summary>
/// Okulda staj notunu kesin gönderir (Draft → Submitted) — #171.
///
/// <para><b>Fiş üretmez.</b> İşletme akışındaki <c>SubmitStudentTermGrade</c>
/// <c>StudentTermGradeSubmitted</c> olayını yayınlar ve Reporting o olaydan Form 8'in kaynağını
/// kurar. Okulda staj için MEB'in tanımladığı bir form yoktur; bu komut olay YAYINLAMAZ, kayıt
/// yalnız başarı değerlendirmesi için kesinleşir.</para>
/// </summary>
public sealed record SubmitSchoolTermGrade(Guid StudentTermGradeId)
{
    public Guid InstitutionId { get; init; }
}
