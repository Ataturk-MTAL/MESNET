using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.ValueObjects;

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
        if (command.SpecializationCode is not null) student.SpecializationCode = command.SpecializationCode;
        if (command.SpecializationName is not null) student.SpecializationName = command.SpecializationName;
        if (command.ClassYear is not null) student.ClassYear = command.ClassYear.Value;
        if (command.Section is not null) student.Section = command.Section;
        if (command.StudentNumber is not null) student.StudentNumber = command.StudentNumber;
        if (command.PhoneNumber is not null) student.PhoneNumber = command.PhoneNumber;
        if (command.TcKimlikNo is not null) student.TcKimlikNo = command.TcKimlikNo;
        // Kalfalık yeterliği ders yılı içinde kazanılabilir; değişince MESEM 12. sınıf taban
        // ücret oranı %15/%30'dan %50'ye geçer (#83).
        if (command.HasJourneymanQualification is not null)
            student.HasJourneymanQualification = command.HasJourneymanQualification.Value;

        if (command.GuardianName is not null || command.GuardianPhone is not null)
        {
            var existing = student.Guardian;
            student.Guardian = new GuardianInfo(
                command.GuardianName ?? existing?.FullName ?? "",
                existing?.TcKimlikNo ?? "",
                command.GuardianPhone ?? existing?.Phone ?? "",
                existing?.Address ?? "",
                existing?.Relationship ?? "");
        }

        session.Store(student);
    }
}
