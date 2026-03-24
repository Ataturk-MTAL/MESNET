using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class PlaceStudentHandler
{
    public static async Task<(InternshipPlacementDto, StudentPlaced)> Handle(PlaceStudent command, IDocumentSession session)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId);
        if (period is null) throw new DomainException(EnrollmentErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(EnrollmentErrors.AcademicPeriodClosed(command.AcademicPeriodId));

        var student = await session.LoadAsync<StudentProfile>(command.StudentId)
            ?? throw new DomainException(EnrollmentErrors.StudentNotFound(command.StudentId));

        if (!student.Status.CanTransitionTo(StudentStatus.Placed))
            throw new DomainException(
                EnrollmentErrors.InvalidTransition("Öğrenci", student.Status.Slug, StudentStatus.Placed.Slug));

        var business = await session.LoadAsync<BusinessProfileView>(command.BusinessId)
            ?? throw new DomainException(EnrollmentErrors.BusinessNotFound(command.BusinessId));

        if (!business.IsActive)
            throw new DomainException(EnrollmentErrors.BusinessNotActive);

        if (business.AvailableCapacity <= 0)
            throw new DomainException(EnrollmentErrors.BusinessCapacityFull);

        var placement = new InternshipPlacement
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = command.AcademicPeriodId,
            TeacherId = command.TeacherId,
            StudentName = student.FullName,
            BranchCode = student.BranchCode,
            Source = ApplicationSource.InstitutionAssignment
        };

        student.Status = StudentStatus.Placed;

        session.Store(placement);
        session.Store(student);

        // TeacherId varsa isim yükle
        string? teacherName = null;
        if (command.TeacherId.HasValue)
        {
            var teacher = await session.LoadAsync<TeacherProfile>(command.TeacherId.Value);
            teacherName = teacher?.FullName;
        }

        return (placement.ToDto(business.BusinessName, teacherName), new StudentPlaced(
            placement.Id,
            placement.StudentId,
            placement.BusinessId,
            placement.InstitutionId,
            placement.AcademicPeriodId,
            placement.TeacherId,
            placement.PlacedAt,
            StudentName: student.FullName,
            BusinessName: business.BusinessName,
            BranchCode: student.BranchCode,
            BranchName: student.BranchName));
    }
}
