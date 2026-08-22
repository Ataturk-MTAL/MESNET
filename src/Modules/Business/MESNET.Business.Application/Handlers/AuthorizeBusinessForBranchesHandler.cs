using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.Policies;
using MESNET.Business.Core.ReadModels;
using MESNET.Business.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class AuthorizeBusinessForBranchesHandler
{
    public static async Task<BusinessBranchesAuthorized> Handle(
        AuthorizeBusinessForBranches command,
        IDocumentSession session,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId, ct)
            ?? throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        if (business.Status == BusinessStatus.Closed)
            throw new DomainException(BusinessErrors.ClosedBusinessNotAuthorizable(business.Id));

        var requested = (command.Branches ?? [])
            .Select(b => new BranchAuthorizationRequest(b.BranchCode?.Trim() ?? "", b.BasedOnDocumentId))
            .ToList();

        if (requested.Any(r => string.IsNullOrWhiteSpace(r.BranchCode)))
            throw new DomainException(BusinessErrors.BranchCodeRequired());

        await EnsureBranchesAreOfferedAsync(requested, session, ct);
        EnsureDocumentsExist(requested, business);

        var now = DateTime.UtcNow;
        var authorizedBy = currentUser.GetFullName();

        business.AuthorizedBranches = BranchAuthorizationPolicy.Apply(
            business.AuthorizedBranches, requested, authorizedBy, now);

        session.Store(business);

        return new BusinessBranchesAuthorized(
            business.Id,
            business.Name,
            business.ActiveBranchCodes.ToList(),
            authorizedBy,
            now);
    }

    /// <summary>
    /// Alan kodu, kurumun açık alanlarından biri olmalıdır (Institution olaylarından beslenen
    /// <see cref="InstitutionBranchView"/>). Read-model henüz hiç dolmamışsa (taze kurulum,
    /// olay tüketicisi çalışmadan önce) doğrulama atlanır — bilinmeyen bir katalogla her isteği
    /// reddetmek geçişi kilitlerdi.
    /// </summary>
    private static async Task EnsureBranchesAreOfferedAsync(
        List<BranchAuthorizationRequest> requested, IDocumentSession session, CancellationToken ct)
    {
        if (requested.Count == 0) return;

        var offeredCodes = await session.Query<InstitutionBranchView>()
            .Where(v => v.IsActive)
            .Select(v => v.FieldCode)
            .ToListAsync(ct);

        if (offeredCodes.Count == 0) return;

        var unknown = requested
            .Select(r => r.BranchCode)
            .FirstOrDefault(code => !offeredCodes.Contains(code, StringComparer.OrdinalIgnoreCase));

        if (unknown is not null)
            throw new DomainException(BusinessErrors.BranchNotOffered(unknown));
    }

    private static void EnsureDocumentsExist(
        List<BranchAuthorizationRequest> requested, Core.Entities.Business business)
    {
        foreach (var request in requested)
        {
            if (request.BasedOnDocumentId is not { } documentId) continue;
            if (business.Documents.All(d => d.Id != documentId))
                throw new DomainException(BusinessErrors.DocumentNotFound(documentId));
        }
    }
}
