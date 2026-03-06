using Marten;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class GetBranchWorkloadConfigHandler
{
    public static async Task<BranchWorkloadConfig?> Handle(
        GetBranchWorkloadConfig query,
        IQuerySession session,
        CancellationToken cancellationToken)
    {
        return await session.Query<BranchWorkloadConfig>()
            .FirstOrDefaultAsync(c =>
                c.InstitutionId == query.InstitutionId &&
                c.BranchCode == query.BranchCode &&
                c.AcademicPeriodId == query.AcademicPeriodId,
                cancellationToken);
    }
}
