using Marten;
using MESNET.Common.Shared;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Application.Errors;
using MESNET.Reporting.Core.Models;
using MESNET.Reporting.Core.ReadModels;
using MESNET.Reporting.Core.Utilities;
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

        // Okulda staj (#159/#171): MEB Form 8 üretilmez. Fiş "İşletmenin Adı" alanı ve iki
        // işletme imzası (usta öğretici, işletme yetkilisi) taşır; işverensiz staj için ayrı
        // bir form da yoktur. Coordination o notu Reporting'e hiç taşımadığı için buraya
        // normalde ulaşılmaz — bu kontrol, ulaşılırsa anlaşılır bir hata döndürmek içindir.
        if (placement is { BusinessId: null })
            throw new DomainException(ReportingErrors.TermGradeSlipNotAvailableForSchoolPlacement(command.StudentId));

        // İşletme iletişim/yetkili bilgisi — Business olaylarından beslenen yerel read-model (#99).
        // Placement'taki kopya, işletme olayı yerleştirmeden önce geldiyse boş kalabildiği için asıl kaynak budur.
        var businessContact = await session.LoadAsync<BusinessContactReportView>(grades.BusinessId, ct);

        var termAverage = ComputeAverage(grades, command.MakeupTrainingScore, command.SkillCompetitionScore);

        var data = new TermGradeSlipFormData
        {
            StudentId = command.StudentId,
            BusinessId = grades.BusinessId,
            InstitutionId = grades.InstitutionId,
            TeacherId = placement?.TeacherId,
            InstitutionName = command.InstitutionName,
            AcademicYear = command.AcademicYear,
            Semester = command.Semester,
            BusinessName = businessContact?.BusinessName ?? placement?.BusinessName ?? string.Empty,
            BusinessPhone = businessContact?.PhoneNumber ?? placement?.BusinessPhone,
            BusinessEmail = businessContact?.Email ?? placement?.BusinessEmail,
            StudentNumber = placement?.StudentNumber ?? string.Empty,
            StudentFullName = placement?.StudentName ?? string.Empty,
            BranchName = placement?.BranchName ?? string.Empty,
            PracticeGrades = grades.PracticeGrades,
            ServiceGrades = grades.ServiceGrades,
            ProjectGrades = grades.ProjectGrades,
            ExperimentGrades = grades.ExperimentGrades,
            MakeupTrainingScore = command.MakeupTrainingScore,
            SkillCompetitionScore = command.SkillCompetitionScore,
            TermAverage = termAverage,
            TermAverageInWords = FormatAverageInWords(termAverage),
            MasterInstructorName = grades.MasterInstructorName ?? businessContact?.MasterInstructorName,
            BusinessOfficialName = businessContact?.RepresentativeName,
            VicePrincipalName = command.VicePrincipalName,
            PrincipalName = command.PrincipalName,
        };

        // Mevcut üretim/depolama (MinIO + GeneratedDocument) yolunu yeniden kullan (aynı modül)
        return await bus.InvokeAsync<Guid>(new GenerateTermGradeSlipDocument(data, command.User));
    }

    /// <summary>
    /// "Ort. (Yazı ile)" hücresi — KARAR (#99): yalnız tam sayı kısmı yazılır, ondalık atılır.
    /// Bozuk veriyle (0–999 dışı ortalama) belge üretimi patlamasın; hücre boş bırakılır.
    /// </summary>
    private static string? FormatAverageInWords(decimal? average)
    {
        if (!average.HasValue) return null;

        var wholePart = (int)Math.Floor(average.Value);
        return wholePart is >= TurkishNumberWords.MinSupportedValue and <= TurkishNumberWords.MaxSupportedValue
            ? TurkishNumberWords.ToWords(wholePart)
            : null;
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
