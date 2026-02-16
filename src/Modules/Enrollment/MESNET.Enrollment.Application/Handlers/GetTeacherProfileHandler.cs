using Marten;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

public static class GetTeacherProfileHandler
{
    public static async Task<TeacherProfileDto?> Handle(GetTeacherProfile query, IQuerySession session)
    {
        var teacher = await session.LoadAsync<TeacherProfile>(query.TeacherId);
        return teacher?.ToDto();
    }
}
