using Marten;
using MESNET.Common.Shared.Security;
using MESNET.Enrollment.Core.Entities;
using MESNET.Security.Shared.Events;

namespace MESNET.Enrollment.Application.Consumers;

public static class UserCreatedConsumer
{
    public static async Task Consume(UserCreated @event, IDocumentSession session)
    {
        // Teacher rolüyse → TeacherProfile oluştur
        if (@event.Roles.Contains(MesnetRoles.Teacher))
        {
            var existing = await session.Query<TeacherProfile>()
                .Where(t => t.KeycloakUserId == Guid.Parse(@event.KeycloakUserId))
                .FirstOrDefaultAsync();

            if (existing is null && @event.InstitutionId.HasValue)
            {
                var teacher = new TeacherProfile
                {
                    Id = Guid.NewGuid(),
                    KeycloakUserId = Guid.Parse(@event.KeycloakUserId),
                    FullName = @event.FullName,
                    InstitutionId = @event.InstitutionId.Value
                };
                session.Store(teacher);
            }
        }

        // Student rolüyse → StudentProfile oluştur (Metadata'dan BranchCode, ClassYear vs.)
        if (@event.Roles.Contains(MesnetRoles.Student))
        {
            var existing = await session.Query<StudentProfile>()
                .Where(s => s.KeycloakUserId == Guid.Parse(@event.KeycloakUserId))
                .FirstOrDefaultAsync();

            if (existing is null && @event.InstitutionId.HasValue)
            {
                var student = new StudentProfile
                {
                    Id = Guid.NewGuid(),
                    KeycloakUserId = Guid.Parse(@event.KeycloakUserId),
                    FullName = @event.FullName,
                    InstitutionId = @event.InstitutionId.Value,
                    BranchCode = @event.Metadata.GetValueOrDefault("BranchCode", ""),
                    BranchName = @event.Metadata.GetValueOrDefault("BranchName", ""),
                    ClassYear = int.TryParse(@event.Metadata.GetValueOrDefault("ClassYear", "0"), out var cy) ? cy : 0
                };
                session.Store(student);
            }
        }
    }
}
