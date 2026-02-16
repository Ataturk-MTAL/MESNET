using Marten;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

public static class GetStudentProfileHandler
{
    public static async Task<StudentProfileDto?> Handle(GetStudentProfile query, IQuerySession session)
    {
        var student = await session.LoadAsync<StudentProfile>(query.StudentId);
        return student?.ToDto();
    }
}
