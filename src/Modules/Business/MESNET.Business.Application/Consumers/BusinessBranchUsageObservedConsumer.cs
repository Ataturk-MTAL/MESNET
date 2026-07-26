using Marten;
using MESNET.Business.Core.Policies;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Business.Application.Consumers;

/// <summary>
/// Geçiş dolgusu (#119): Enrollment'ın bildirdiği fiilî alan kullanımından EKSİK alan yetkilerini
/// üretir. Hiçbir yetkiyi iptal etmez — dolgu yalnız ekler.
/// </summary>
public static class BusinessBranchUsageObservedConsumer
{
    /// <summary>Dolgunun ürettiği yetkilerin "onaylayan" bilgisi — idari onaydan ayırt edilebilsin.</summary>
    private const string BackfillAuthor = "Sistem (geçiş dolgusu)";

    public static async Task<BusinessBranchesAuthorized?> Consume(
        BusinessBranchUsageObserved @event, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(@event.BusinessId);
        if (business is null) return null;

        business.AuthorizedBranches = BranchAuthorizationPolicy.Merge(
            business.AuthorizedBranches, @event.BranchCodes, BackfillAuthor, @event.ObservedAt);

        session.Store(business);

        // Değişiklik olmasa bile yayınlanır: dolgunun asıl amacı tüketici modüllerin
        // (Enrollment guard read-model'i, Coordination) boş kalan kopyalarını doldurmaktır.
        return new BusinessBranchesAuthorized(
            business.Id,
            business.Name,
            business.ActiveBranchCodes.ToList(),
            BackfillAuthor,
            @event.ObservedAt);
    }
}
