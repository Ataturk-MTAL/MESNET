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

        var student = await session.LoadAsync<StudentProfile>(placement.StudentId);
        var business = await session.LoadAsync<BusinessProfileView>(placement.BusinessId);
        TeacherProfile? teacher = placement.TeacherId.HasValue
            ? await session.LoadAsync<TeacherProfile>(placement.TeacherId.Value)
            : null;

        return placement.ToDto(
            student?.FullName ?? "",
            business?.BusinessName ?? "",
            teacher?.FullName);
    }
}
