using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.Policies;
using MESNET.Enrollment.Shared.Events;
using Wolverine;

namespace MESNET.Enrollment.Application.Handlers;

public sealed record SyncStudentCountsResult(List<StudentCountsSynced> Events);

public static class SyncStudentCountsHandler
{
    public static async Task<SyncStudentCountsResult> Handle(
        SyncStudentCounts command,
        IQuerySession session,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        // Marten SmartEnum LINQ tuzağı: Status filtreleme/projection yapılamaz.
        // Sadece institution+period filtresi ile çekip entity üzerinden in-memory filtrele.
        var allStudents = await session.Query<StudentProfile>()
            .Where(s =>
                s.InstitutionId == command.InstitutionId &&
                s.AcademicPeriodId == command.AcademicPeriodId)
            .ToListAsync(cancellationToken);

        // Karar saf StudentCountPolicy'dedir; handler yalnız girdi toplar ve olaya çevirir.
        // Gruplama TÜM öğrenciler üzerinden, sayım yalnız aktifler üzerinden yapılır —
        // gerekçesi ve ölçümü politikanın belgesinde (#290).
        var events = StudentCountPolicy.ActiveCountsByBranch(allStudents)
            .Select(c => new StudentCountsSynced(
                command.InstitutionId,
                command.AcademicPeriodId,
                c.BranchCode,
                c.EducationTypeName,
                c.Counts))
            .ToList();

        // Event yayınlama handler'ın işidir (cross-module → PublishAsync). Endpoint artık publish ETMEZ.
        foreach (var e in events)
            await bus.PublishAsync(e);

        return new SyncStudentCountsResult(events);
    }
}
