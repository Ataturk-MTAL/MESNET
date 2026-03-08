using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class DeregisterStudentHandler
{
    public static async Task<StudentDeregistered> Handle(
        DeregisterStudent command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var student = await session.LoadAsync<StudentProfile>(command.StudentId, cancellationToken);
        if (student is null)
            throw new DomainException(EnrollmentErrors.StudentNotFound(command.StudentId));

        if (student.Status == StudentStatus.ActiveInternship)
            throw new DomainException(EnrollmentErrors.CannotDeregisterActiveInternship(command.StudentId));

        if (!student.Status.CanTransitionTo(StudentStatus.Deregistered))
            throw new DomainException(EnrollmentErrors.InvalidTransition(
                "Öğrenci", student.Status.Slug, StudentStatus.Deregistered.Slug));

        student.Status = StudentStatus.Deregistered;
        session.Store(student);

        return new StudentDeregistered(
            student.Id,
            student.InstitutionId,
            student.AcademicPeriodId,
            student.BranchCode,
            student.ClassYear,
            student.EducationType.Name,
            command.Reason);
    }
}
