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

        if (command.FullName is not null) student.FullName = command.FullName;
        if (command.BranchCode is not null) student.BranchCode = command.BranchCode;
        if (command.BranchName is not null) student.BranchName = command.BranchName;
        if (command.ClassYear is not null) student.ClassYear = command.ClassYear.Value;
        if (command.Section is not null) student.Section = command.Section;

        session.Store(student);
    }
}
