using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class ApplyForInternshipHandler
{
    public static async Task<(Guid, InternshipApplied)> Handle(ApplyForInternship command, IDocumentSession session)
    {
        var student = await session.LoadAsync<StudentProfile>(command.StudentId)
            ?? throw new DomainException(EnrollmentErrors.StudentNotFound(command.StudentId));

        if (!student.Status.CanTransitionTo(StudentStatus.Applied))
            throw new DomainException(
                EnrollmentErrors.InvalidTransition("Öğrenci", student.Status.Slug, StudentStatus.Applied.Slug));

        student.Status = StudentStatus.Applied;
        session.Store(student);

        return (student.Id, new InternshipApplied(
            student.Id,
            command.BusinessId,
            command.BranchCode,
            ApplicationSource.StudentApplication.Name));
    }
}
