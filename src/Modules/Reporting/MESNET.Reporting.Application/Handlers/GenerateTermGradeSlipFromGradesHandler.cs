using Marten;
using MESNET.Common.Shared;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Application.Errors;
using MESNET.Reporting.Core.Models;
using MESNET.Reporting.Core.ReadModels;
using Wolverine;

namespace MESNET.Reporting.Application.Handlers;

public static class GenerateTermGradeSlipFromGradesHandler
{
    public static async Task<Guid> Handle(
        GenerateTermGradeSlipFromGrades command,
        IQuerySession session,
        IMessageBus bus,
        CancellationToken ct)
    {
        // İşletmenin gönderdiği notlar (zorunlu)
        var grades = await session.Query<StudentTermGradeView>()
            .Where(g => g.StudentId == command.StudentId && g.AcademicPeriodId == command.AcademicPeriodId)
            .FirstOrDefaultAsync(ct)
            ?? throw new DomainException(ReportingErrors.TermGradesNotFound(command.StudentId));

        // Ortak alanlar (öğrenci/işletme) — mevcut placement read-model'inden zenginleştir
        var placement = await session.Query<StudentPlacementReportView>()
            .Where(p => p.StudentId == command.StudentId && p.AcademicPeriodId == command.AcademicPeriodId)
            .FirstOrDefaultAsync(ct);

        var data = new TermGradeSlipFormData
        {
            StudentId = command.StudentId,
            BusinessId = grades.BusinessId,
            InstitutionId = grades.InstitutionId,
            TeacherId = placement?.TeacherId,
            InstitutionName = command.InstitutionName,
            AcademicYear = command.AcademicYear,
            Semester = command.Semester,
            BusinessName = placement?.BusinessName ?? string.Empty,
            BusinessPhone = placement?.BusinessPhone,
            BusinessEmail = placement?.BusinessEmail,
            StudentNumber = placement?.StudentNumber ?? string.Empty,
            StudentFullName = placement?.StudentName ?? string.Empty,
            BranchName = placement?.BranchName ?? string.Empty,
            PracticeGrades = grades.PracticeGrades,
            ServiceGrades = grades.ServiceGrades,
            ProjectGrades = grades.ProjectGrades,
            ExperimentGrades = grades.ExperimentGrades,
            MakeupTrainingScore = command.MakeupTrainingScore,
            SkillCompetitionScore = command.SkillCompetitionScore,
            TermAverage = ComputeAverage(grades, command.MakeupTrainingScore, command.SkillCompetitionScore),
            MasterInstructorName = grades.MasterInstructorName,
            BusinessOfficialName = placement?.BusinessContactName,
            VicePrincipalName = command.VicePrincipalName,
            PrincipalName = command.PrincipalName,
        };

        // Mevcut üretim/depolama (MinIO + GeneratedDocument) yolunu yeniden kullan (aynı modül)
        return await bus.InvokeAsync<Guid>(new GenerateTermGradeSlipDocument(data, command.User));
    }

    // Otomatik dönem ortalaması — 4 işletme kategorisi + okul-payı (*) puanlar dahil
    private static decimal? ComputeAverage(StudentTermGradeView g, int? makeup, int? skill)
    {
        var all = g.PracticeGrades
            .Concat(g.ServiceGrades)
            .Concat(g.ProjectGrades)
            .Concat(g.ExperimentGrades)
            .ToList();
        if (makeup.HasValue) all.Add(makeup.Value);
        if (skill.HasValue) all.Add(skill.Value);
        return all.Count > 0 ? Math.Round((decimal)all.Average(), 2) : null;
    }
}
