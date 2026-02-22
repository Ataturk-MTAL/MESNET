using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class RegisterStudentHandler
{
    public static async Task<(StudentProfileDto, StudentRegistered)> Handle(RegisterStudent command, IDocumentSession session)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId);
        if (period is null) throw new DomainException(EnrollmentErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(EnrollmentErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        var student = new StudentProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = command.AcademicPeriodId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName,
            BranchCode = command.BranchCode,
            BranchName = command.BranchName,
            ClassYear = command.ClassYear,
            Section = command.Section
        };

        session.Store(student);

        return (student.ToDto(), new StudentRegistered(student.Id, student.FullName, student.InstitutionId, student.AcademicPeriodId, student.BranchCode));
    }
}
