using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class SyncStudentCountsHandler
{
    public static async Task<IEnumerable<StudentCountsSynced>> Handle(
        SyncStudentCounts command,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        // Kayıt silindi ve tamamladı hariç tüm öğrencileri say
        var students = await session.Query<StudentProfile>()
            .Where(s =>
                s.InstitutionId == command.InstitutionId &&
                s.AcademicPeriodId == command.AcademicPeriodId &&
                s.Status != StudentStatus.Deregistered &&
                s.Status != StudentStatus.Completed)
            .Select(s => new { s.BranchCode, s.EducationTypeName, s.ClassYear })
            .ToListAsync(cancellationToken);

        // Alan + Eğitim tipi bazında grupla
        var grouped = students
            .GroupBy(s => new { s.BranchCode, s.EducationTypeName })
            .Select(g => new StudentCountsSynced(
                command.InstitutionId,
                command.AcademicPeriodId,
                g.Key.BranchCode,
                g.Key.EducationTypeName,
                g.GroupBy(s => s.ClassYear)
                    .ToDictionary(cg => cg.Key, cg => cg.Count())));

        return grouped;
    }
}
