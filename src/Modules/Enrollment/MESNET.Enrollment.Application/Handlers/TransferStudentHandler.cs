using Marten;
using MESNET.Enrollment.Application.Commands;
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
            ?? throw new InvalidOperationException($"Yerleştirme bulunamadı: {command.PlacementId}");

        if (!oldPlacement.Status.CanTransitionTo(PlacementStatus.Transferred))
            throw new InvalidOperationException(
                $"Yerleştirme '{oldPlacement.Status.Slug}' durumundan '{PlacementStatus.Transferred.Slug}' durumuna geçirilemez.");

        var newBusiness = await session.LoadAsync<BusinessProfileView>(command.NewBusinessId)
            ?? throw new InvalidOperationException($"Yeni işletme bulunamadı: {command.NewBusinessId}");

        if (!newBusiness.IsActive)
            throw new InvalidOperationException("Yeni işletme aktif değil, transfer yapılamaz.");

        if (newBusiness.AvailableCapacity <= 0)
            throw new InvalidOperationException("Yeni işletme kapasitesi dolu, transfer yapılamaz.");

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
