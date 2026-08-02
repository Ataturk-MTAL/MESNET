using Marten;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class StudentTermGradeQueryHandler
{
    // İşletmenin kendi öğrencileri + (varsa) mevcut not durumu — not girişi ekranı
    public static async Task<TermGradeRowsResult> Handle(
        GetMyStudentsForGrading query, IQuerySession session, CancellationToken ct)
    {
        var students = await session.Query<CoordinationPlacedStudentView>()
            .Where(p => p.BusinessId == query.BusinessId
                        && p.AcademicPeriodId == query.AcademicPeriodId
                        && p.IsActive)
            .ToListAsync(ct);

        var studentIds = students.Select(s => s.StudentId).ToList();
        var grades = await session.Query<StudentTermGrade>()
            .Where(g => g.AcademicPeriodId == query.AcademicPeriodId && studentIds.Contains(g.StudentId))
            .ToListAsync(ct);
        var byStudent = grades.ToDictionary(g => g.StudentId);

        var rows = students
            .OrderBy(s => s.StudentName)
            .Select(s => ToRow(s.StudentId, s.StudentName, s.BranchName, byStudent.GetValueOrDefault(s.StudentId)))
            .ToList();
        return new TermGradeRowsResult(rows);
    }

    // Okulda staj yapan öğrenciler + (varsa) mevcut not durumu — okul tarafı not girişi (#171)
    public static async Task<TermGradeRowsResult> Handle(
        GetSchoolStudentsForGrading query, IQuerySession session, CancellationToken ct)
    {
        var students = await session.Query<SchoolPlacedStudentView>()
            .Where(p => p.InstitutionId == query.InstitutionId
                        && p.AcademicPeriodId == query.AcademicPeriodId
                        && p.IsActive)
            .ToListAsync(ct);

        var studentIds = students.Select(s => s.StudentId).ToList();
        var grades = await session.Query<StudentTermGrade>()
            .Where(g => g.AcademicPeriodId == query.AcademicPeriodId && studentIds.Contains(g.StudentId))
            .ToListAsync(ct);
        var byStudent = grades.ToDictionary(g => g.StudentId);

        var rows = students
            .OrderBy(s => s.StudentName)
            .Select(s => ToRow(s.StudentId, s.StudentName, s.BranchName, byStudent.GetValueOrDefault(s.StudentId)))
            .ToList();
        return new TermGradeRowsResult(rows);
    }

    // Koordinatör/okul için GÖNDERİLMİŞ notlar (fiş üretilecekler)
    public static async Task<TermGradeRowsResult> Handle(
        GetSubmittedTermGrades query, IQuerySession session, CancellationToken ct)
    {
        // Okulda staj notları bu listeye GİRMEZ (#171): liste "fiş üretilecekler"dir ve o hâl
        // için MEB Form 8 üretilmez. Filtre olmasaydı koordinatör ekranda görür ve üretmeyi
        // denerdi — işletme adı ve iki işletme imzası olmayan bir fiş.
        var grades = await session.Query<StudentTermGrade>()
            .Where(g => g.InstitutionId == query.InstitutionId
                        && g.AcademicPeriodId == query.AcademicPeriodId
                        && g.BusinessId != null
                        && g.StatusName == StudentTermGradeStatus.Submitted.Name)
            .ToListAsync(ct);

        var rows = grades
            .OrderBy(g => g.StudentName)
            .Select(g => ToRow(g.StudentId, g.StudentName, g.BranchName, g))
            .ToList();
        return new TermGradeRowsResult(rows);
    }

    private static StudentGradeRowDto ToRow(Guid studentId, string name, string branch, StudentTermGrade? g) =>
        new(studentId, name, branch,
            g?.Id, g?.StatusName, g?.Status.Slug,
            g?.PracticeGrades ?? [], g?.ServiceGrades ?? [], g?.ProjectGrades ?? [], g?.ExperimentGrades ?? [],
            g?.MasterInstructorName, g?.TermAverage);
}
