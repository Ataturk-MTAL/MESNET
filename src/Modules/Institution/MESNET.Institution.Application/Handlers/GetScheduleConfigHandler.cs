using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Application.Queries;

namespace MESNET.Institution.Application.Handlers;

public static class GetScheduleConfigHandler
{
    public static async Task<ScheduleConfigDto> Handle(GetScheduleConfig query, IQuerySession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(query.InstitutionId);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(query.InstitutionId));

        if (institution.ScheduleConfig is null)
            return new ScheduleConfigDto(false, null, null, null);

        return new ScheduleConfigDto(
            true,
            institution.ScheduleConfig.DailyPeriodCount,
            institution.ScheduleConfig.UpdatedAt,
            institution.ScheduleConfig.UpdatedBy);
    }
}
