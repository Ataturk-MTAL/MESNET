using Marten;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

public static class GetMyTeacherProfileHandler
{
    public static async Task<TeacherProfileDto?> Handle(GetMyTeacherProfile query, IQuerySession session)
    {
        if (query.KeycloakUserId == Guid.Empty) return null;

        // Session kiracılıdır: yalnız aktif okuldaki kayıt bulunur.
        var teacher = await session.Query<TeacherProfile>()
            .FirstOrDefaultAsync(t => t.KeycloakUserId == query.KeycloakUserId);
        return teacher?.ToDto();
    }
}
