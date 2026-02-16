using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class RegisterStudentHandler
{
    public static StudentRegistered Handle(RegisterStudent command, IDocumentSession session)
    {
        var student = new StudentProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName,
            BranchCode = command.BranchCode,
            BranchName = command.BranchName,
            ClassYear = command.ClassYear
        };

        session.Store(student);

        return new StudentRegistered(student.Id, student.FullName, student.InstitutionId, student.BranchCode);
    }
}
