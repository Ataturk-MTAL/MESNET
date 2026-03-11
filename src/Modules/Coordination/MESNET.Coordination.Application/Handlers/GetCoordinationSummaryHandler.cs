using Marten;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class GetCoordinationSummaryHandler
{
    public static async Task<CoordinationSummaryDto> Handle(
        GetCoordinationSummary query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        IQueryable<BusinessCoordinationView> queryable = session.Query<BusinessCoordinationView>()
            .Where(v => v.InstitutionId == query.InstitutionId);

        if (!string.IsNullOrWhiteSpace(query.BranchCode))
            queryable = queryable.Where(v => v.BranchCode == query.BranchCode);

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(v => v.AcademicPeriodId == query.AcademicPeriodId.Value);

        var views = await queryable.ToListAsync(cancellationToken);

        var totalMaxHours = views.Sum(v => v.MaxCoordinationHours);
        var totalAssigned = views.Sum(v => v.AssignedHours);
        var assignedCount = views.Count(v => v.AssignedTeacherId is not null);
        var unassignedCount = views.Count(v => v.AssignedTeacherId is null);

        // Ders yükü havuzunu BranchWorkloadConfig'den oku
        var totalWorkloadPool = 0;
        if (!string.IsNullOrWhiteSpace(query.BranchCode) && query.AcademicPeriodId.HasValue)
        {
            var workloadConfig = await session.Query<BranchWorkloadConfig>()
                .FirstOrDefaultAsync(c =>
                    c.InstitutionId == query.InstitutionId &&
                    c.BranchCode == query.BranchCode &&
                    c.AcademicPeriodId == query.AcademicPeriodId.Value,
                    cancellationToken);

            totalWorkloadPool = workloadConfig?.TotalWorkloadPool ?? 0;
        }

        // Öğretmen bazlı özet
        var teacherWorkloads = views
            .Where(v => v.AssignedTeacherId is not null)
            .GroupBy(v => v.AssignedTeacherId!.Value)
            .Select(g => new TeacherWorkloadSummaryDto(
                g.Key,
                g.First().AssignedTeacherName ?? "",
                g.Sum(v => v.AssignedHours),
                g.Count()
            ))
            .OrderByDescending(t => t.AssignedHours)
            .ToList();

        return new CoordinationSummaryDto(
            totalWorkloadPool,
            totalAssigned,
            totalWorkloadPool - totalAssigned,
            totalMaxHours,
            assignedCount,
            unassignedCount,
            teacherWorkloads
        );
    }
}
