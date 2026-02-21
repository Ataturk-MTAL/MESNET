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
            TeacherId = command.TeacherId,
            Source = ApplicationSource.InstitutionAssignment
        };

        student.Status = StudentStatus.Placed;

        session.Store(placement);
        session.Store(student);

        return (placement.ToDto(), new StudentPlaced(
            placement.Id,
            placement.StudentId,
            placement.BusinessId,
            placement.InstitutionId,
            placement.PlacedAt));
    }
}
