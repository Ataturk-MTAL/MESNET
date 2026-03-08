using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public sealed record SyncStudentCountsResult(List<StudentCountsSynced> Events);

public static class SyncStudentCountsHandler
{
    public static async Task<SyncStudentCountsResult> Handle(
        SyncStudentCounts command,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        // Marten SmartEnum LINQ tuzağı: Status filtreleme/projection yapılamaz.
        // Sadece institution+period filtresi ile çekip entity üzerinden in-memory filtrele.
        var allStudents = await session.Query<StudentProfile>()
            .Where(s =>
                s.InstitutionId == command.InstitutionId &&
                s.AcademicPeriodId == command.AcademicPeriodId)
            .ToListAsync(cancellationToken);

        // Kayıt silindi ve tamamladı hariç aktif öğrencileri filtrele
        var active = allStudents
            .Where(s => !s.Status.IsFinal);

        var events = active
            .GroupBy(s => new { s.BranchCode, s.EducationTypeName })
            .Select(g => new StudentCountsSynced(
                command.InstitutionId,
                command.AcademicPeriodId,
                g.Key.BranchCode,
                g.Key.EducationTypeName,
                g.GroupBy(s => s.ClassYear)
                    .ToDictionary(cg => cg.Key, cg => cg.Count())))
            .ToList();

        return new SyncStudentCountsResult(events);
    }
}
