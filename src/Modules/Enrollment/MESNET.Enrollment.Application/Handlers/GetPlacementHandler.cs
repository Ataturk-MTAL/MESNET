using Marten;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.ReadModels;

namespace MESNET.Enrollment.Application.Handlers;

public static class GetPlacementHandler
{
    public static async Task<InternshipPlacementDto?> Handle(GetPlacement query, IQuerySession session)
    {
        var placement = await session.LoadAsync<InternshipPlacement>(query.PlacementId);
        if (placement is null) return null;

        var business = placement.BusinessId is { } bid ? await session.LoadAsync<BusinessProfileView>(bid) : null;
        TeacherProfile? teacher = placement.TeacherId.HasValue
            ? await session.LoadAsync<TeacherProfile>(placement.TeacherId.Value)
            : null;

        return placement.ToDto(
            business?.BusinessName ?? "",
            teacher?.FullName);
    }
}
