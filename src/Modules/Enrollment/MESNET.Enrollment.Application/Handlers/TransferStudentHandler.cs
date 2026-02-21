using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class TransferStudentHandler
{
    public static async Task<StudentTransferred> Handle(TransferStudent command, IDocumentSession session)
    {
        var oldPlacement = await session.LoadAsync<InternshipPlacement>(command.PlacementId)
            ?? throw new DomainException(EnrollmentErrors.PlacementNotFound(command.PlacementId));

        if (!oldPlacement.Status.CanTransitionTo(PlacementStatus.Transferred))
            throw new DomainException(
                EnrollmentErrors.InvalidTransition("Yerleştirme", oldPlacement.Status.Slug, PlacementStatus.Transferred.Slug));

        var newBusiness = await session.LoadAsync<BusinessProfileView>(command.NewBusinessId)
            ?? throw new DomainException(EnrollmentErrors.BusinessNotFound(command.NewBusinessId));

        if (!newBusiness.IsActive)
            throw new DomainException(EnrollmentErrors.BusinessNotActive);

        if (newBusiness.AvailableCapacity <= 0)
            throw new DomainException(EnrollmentErrors.BusinessCapacityFull);

        oldPlacement.Status = PlacementStatus.Transferred;
        oldPlacement.TransferredAt = DateTime.UtcNow;
        oldPlacement.TransferReason = command.Reason;

        var newPlacement = new InternshipPlacement
        {
            Id = Guid.NewGuid(),
            StudentId = oldPlacement.StudentId,
            BusinessId = command.NewBusinessId,
            InstitutionId = oldPlacement.InstitutionId,
            TeacherId = oldPlacement.TeacherId,
            Source = ApplicationSource.InstitutionAssignment
        };

        session.Store(oldPlacement);
        session.Store(newPlacement);

        return new StudentTransferred(
            oldPlacement.Id,
            oldPlacement.StudentId,
            oldPlacement.BusinessId,
            command.NewBusinessId,
            command.Reason);
    }
}
