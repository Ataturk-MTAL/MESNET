using Marten;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Entities;

namespace MESNET.Institution.Application.Handlers;

public static class ListAcademicPeriodsHandler
{
    public static async Task<IReadOnlyList<AcademicPeriodDto>> Handle(
        ListAcademicPeriods query, IQuerySession session)
    {
        var periods = await session.Query<AcademicPeriod>()
            .Where(p => p.InstitutionId == query.InstitutionId)
            .ToListAsync();

        return periods.Select(p => p.ToDto()).ToList();
    }
}
