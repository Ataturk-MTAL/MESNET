using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class GetActivityReportHandler
{
    public static async Task<MonthlyActivityReport> Handle(
        GetActivityReport query, IQuerySession session)
    {
        var report = await session.LoadAsync<MonthlyActivityReport>(query.ReportId);
        if (report is null)
            throw new DomainException(new Error("MonthlyActivityReport.NotFound", $"Aylık faaliyet raporu bulunamadı: {query.ReportId}"));

        return report;
    }
}
