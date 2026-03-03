using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class RegisterTeacherHandler
{
    public static (TeacherProfileDto, TeacherRegistered) Handle(RegisterTeacher command, IDocumentSession session)
    {
        var teacher = new TeacherProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName,
            BranchCode = command.BranchCode
        };

        session.Store(teacher);

        return (teacher.ToDto(), new TeacherRegistered(teacher.Id, teacher.FullName, teacher.InstitutionId));
    }
}
