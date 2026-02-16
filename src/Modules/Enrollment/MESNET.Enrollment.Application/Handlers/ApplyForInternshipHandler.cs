using Marten;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class ApplyForInternshipHandler
{
    public static async Task<InternshipApplied> Handle(ApplyForInternship command, IDocumentSession session)
    {
        var student = await session.LoadAsync<StudentProfile>(command.StudentId)
            ?? throw new InvalidOperationException($"Öğrenci bulunamadı: {command.StudentId}");

        if (!student.Status.CanTransitionTo(StudentStatus.Applied))
            throw new InvalidOperationException(
                $"Öğrenci '{student.Status.Slug}' durumundan '{StudentStatus.Applied.Slug}' durumuna geçirilemez.");

        student.Status = StudentStatus.Applied;
        session.Store(student);

        return new InternshipApplied(
            student.Id,
            command.BusinessId,
            command.BranchCode,
            ApplicationSource.StudentApplication.Name);
    }
}
