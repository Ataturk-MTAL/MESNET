using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.Enums;

namespace MESNET.Institution.Application.Handlers;

public static class GetActiveAcademicPeriodHandler
{
    public static async Task<AcademicPeriodDto> Handle(
        GetActiveAcademicPeriod query, IQuerySession session)
    {
        // SmartEnum LINQ kısıtı: tüm dönemleri çek, in-memory filtrele
        var periods = await session.Query<AcademicPeriod>()
            .Where(p => p.InstitutionId == query.InstitutionId)
            .ToListAsync();

        var active = periods.FirstOrDefault(p => p.Status.Name == AcademicPeriodStatus.Active.Name)
            ?? throw new DomainException(InstitutionErrors.NoActiveAcademicPeriod(query.InstitutionId));

        return active.ToDto();
    }
}
