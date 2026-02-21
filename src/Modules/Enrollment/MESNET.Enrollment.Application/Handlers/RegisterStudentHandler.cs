using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class RegisterStudentHandler
{
    public static (StudentProfileDto, StudentRegistered) Handle(RegisterStudent command, IDocumentSession session)
    {
        var student = new StudentProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName,
            BranchCode = command.BranchCode,
            BranchName = command.BranchName,
            ClassYear = command.ClassYear,
            Section = command.Section
        };

        session.Store(student);

        return (student.ToDto(), new StudentRegistered(student.Id, student.FullName, student.InstitutionId, student.BranchCode));
    }
}
