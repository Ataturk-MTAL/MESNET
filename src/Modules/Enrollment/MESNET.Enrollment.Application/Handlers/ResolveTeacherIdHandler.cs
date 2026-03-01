using Marten;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

public static class ResolveTeacherIdHandler
{
    public static async Task<Guid?> Handle(ResolveTeacherId query, IQuerySession session)
    {
        var teacher = await session.Query<TeacherProfile>()
            .FirstOrDefaultAsync(t => t.KeycloakUserId == query.KeycloakUserId);
        return teacher?.Id;
    }
}
