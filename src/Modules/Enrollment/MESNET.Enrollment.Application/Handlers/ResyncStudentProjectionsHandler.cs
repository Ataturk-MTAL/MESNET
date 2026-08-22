using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Shared.Events;
using Wolverine;

namespace MESNET.Enrollment.Application.Handlers;

public static class ResyncStudentProjectionsHandler
{
    public static async Task<ResyncStudentProjectionsResult> Handle(
        ResyncStudentProjections command, IQuerySession session, IMessageBus bus, CancellationToken ct)
    {
        var students = await session.Query<StudentProfile>().ToListAsync(ct);

        // Her öğrenci için StudentRegistered'ı yeniden yayınla (cross-module → PublishAsync).
        // Tüketen modüllerin read-model consumer'ları idempotent upsert yapar; SmartEnum alanı
        // event sözleşmesinde string olduğundan .Name geçilir (CLAUDE.md cross-module event kuralı).
        foreach (var s in students)
        {
            await bus.PublishAsync(new StudentRegistered(
                s.Id,
                s.FullName,
                s.InstitutionId,
                s.AcademicPeriodId,
                s.BranchCode,
                s.ClassYear,
                s.EducationType.Name,
                s.StudentNumber ?? "",
                s.HasJourneymanQualification,
                s.BirthDate,
                s.Category.Name,
                // #230 — mevcut öğrencilerin StudentId otoritesi bu resync ile dolar.
                s.KeycloakUserId));
        }

        return new ResyncStudentProjectionsResult(students.Count);
    }
}
