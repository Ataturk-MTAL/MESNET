using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Application.Errors;
using MESNET.Internship.Core.Entities;

namespace MESNET.Internship.Application.Handlers;

/// <summary>
/// Eşiği yazar. Tek satırlık ulusal parametre — sürüm geçmişi yok; eşik sorgu ANINDA
/// değerlendirilir, geriye dönük hesap yoktur.
/// </summary>
public static class UpdateApprovalConfigHandler
{
    public static async Task Handle(
        UpdateApprovalConfig command,
        IDocumentSession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        if (!InternshipApprovalConfig.IsValidThreshold(command.StuckApprovalDays))
            throw new DomainException(
                InternshipErrors.InvalidStuckApprovalThreshold(command.StuckApprovalDays));

        var config = await session.LoadAsync<InternshipApprovalConfig>(
                         InternshipApprovalConfig.SingletonId, cancellationToken)
                     ?? new InternshipApprovalConfig();

        config.StuckApprovalDays = command.StuckApprovalDays;
        config.UpdatedById = currentUser.GetUserId();
        config.UpdatedAt = DateTime.UtcNow;

        session.Store(config);
    }
}
