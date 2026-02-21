using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

public static class UpdateStudentProfileHandler
{
    public static async Task Handle(UpdateStudentProfile command, IDocumentSession session)
    {
        var student = await session.LoadAsync<StudentProfile>(command.StudentId)
            ?? throw new DomainException(EnrollmentErrors.StudentNotFound(command.StudentId));

        student.FullName = command.FullName;
        student.BranchCode = command.BranchCode;
        student.BranchName = command.BranchName;
        student.ClassYear = command.ClassYear;
        student.Section = command.Section;

        session.Store(student);
    }
}
