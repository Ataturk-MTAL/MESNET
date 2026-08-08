using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Enrollment.Application.Handlers;

public static class MarkAsFailedToCompleteHandler
{
    public static async Task<StudentFailedToComplete> Handle(MarkAsFailedToComplete command, IDocumentSession session)
    {
        var placement = await session.LoadAsync<InternshipPlacement>(command.PlacementId)
            ?? throw new DomainException(EnrollmentErrors.PlacementNotFound(command.PlacementId));

        if (placement.InstitutionId != command.InstitutionId)
            throw new DomainException(EnrollmentErrors.PlacementNotFound(command.PlacementId));

        if (!placement.Status.CanTransitionTo(PlacementStatus.FailedToComplete))
            throw new DomainException(
                EnrollmentErrors.InvalidTransition("Staj", placement.Status.Slug, PlacementStatus.FailedToComplete.Slug));

        placement.Status = PlacementStatus.FailedToComplete;

        session.Store(placement);

        return new StudentFailedToComplete(
            placement.Id,
            placement.StudentId,
            placement.BusinessId,
            placement.InstitutionId,
            placement.AcademicPeriodId,
            placement.BranchCode);
    }
}
