using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class RegisterTeacherHandler
{
    public static TeacherRegistered Handle(RegisterTeacher command, IDocumentSession session)
    {
        var teacher = new TeacherProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName
        };

        session.Store(teacher);

        return new TeacherRegistered(teacher.Id, teacher.FullName, teacher.InstitutionId);
    }
}
