using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Core.Entities;

namespace MESNET.Institution.Application.Extensions;

public static class AcademicPeriodMappingExtensions
{
    public static AcademicPeriodDto ToDto(this AcademicPeriod entity) => new(
        entity.Id,
        entity.InstitutionId,
        entity.Name,
        entity.StartYear,
        entity.EndYear,
        entity.StartDate.ToString("yyyy-MM-dd"),
        entity.EndDate.ToString("yyyy-MM-dd"),
        entity.Status.Name,
        entity.Status.Slug,
        entity.CreatedAt.ToString("O"),
        entity.ClosedAt?.ToString("O"));
}
