namespace MESNET.Coordination.Application.Queries;

/// <summary>İşletmenin not girişi için kendi öğrencileri + (varsa) mevcut not durumu. BusinessId token'dan.</summary>
public sealed record GetMyStudentsForGrading(Guid BusinessId, Guid AcademicPeriodId);

/// <summary>
/// Koordinatör/okul için fiş üretilecek GÖNDERİLMİŞ dönem notları. InstitutionId token'dan.
/// Okulda staj notları bu listeye GİRMEZ (#171) — o hâl için fiş üretilmez.
/// </summary>
public sealed record GetSubmittedTermGrades(Guid InstitutionId, Guid AcademicPeriodId);

/// <summary>
/// Okulda staj yapan öğrenciler + (varsa) mevcut not durumu — okul tarafının not giriş
/// ekranı (#171). Kapsam <c>institution_id</c> claim'inden gelir, istekten alınmaz.
/// </summary>
public sealed record GetSchoolStudentsForGrading(Guid InstitutionId, Guid AcademicPeriodId);

/// <summary>Tek öğrencinin not satırı (girilmemişse Status null).</summary>
public sealed record StudentGradeRowDto(
    Guid StudentId,
    string StudentName,
    string BranchName,
    Guid? GradeId,
    string? Status,
    string? StatusSlug,
    List<int> PracticeGrades,
    List<int> ServiceGrades,
    List<int> ProjectGrades,
    List<int> ExperimentGrades,
    string? MasterInstructorName,
    decimal? TermAverage);

// Wolverine IEnumerable-return tuzağı için sonuçları somut tip içine sar (bkz. CLAUDE.md/memory)
public sealed record TermGradeRowsResult(List<StudentGradeRowDto> Students);
