using Marten;
using MESNET.Security.Application.Commands;
using MESNET.Security.Core.Entities;
using MESNET.Security.Shared.Events;
using Wolverine;

namespace MESNET.Security.Application.Handlers;

/// <summary>
/// <see cref="ReplayUserAccounts"/> sonucu — kaç hesap yeniden yayınlandı.
/// </summary>
public sealed record ReplayUserAccountsResult(int Replayed);

/// <summary>
/// <inheritdoc cref="ReplayUserAccounts"/>
/// </summary>
public static class ReplayUserAccountsHandler
{
    public static async Task<ReplayUserAccountsResult> Handle(
        ReplayUserAccounts command,
        IQuerySession session,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        // Silinmiş hesaplar (mezar taşı, #210) yeniden yayınlanmaz: Keycloak'ta artık yoklar
        // ve InstitutionManagerLink'i "yönetiliyor" gösterip aranan tam da o okulu gizlerdi.
        var accounts = await session.Query<UserAccount>()
            .Where(u => u.DeletedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var account in accounts)
        {
            // UserCreated DEĞİL: o olayın Business/Enrollment/Institution personel
            // tüketicileri "yeni kayıt" varsayar ve boş Metadata ile silinmiş kayıtları eksik
            // alanlarla diriltirdi. UserAccountReplayed yalnız InstitutionManagerLink'i
            // besler ve etkinlik durumunu olayın kendisiyle taşır — ayrı bir UserDeactivated
            // yayınına, dolayısıyla iki olay arasındaki sıra garantisine gerek kalmaz.
            await bus.PublishAsync(new UserAccountReplayed(
                account.Id,
                account.KeycloakUserId,
                account.Roles,
                account.InstitutionId,
                account.IsEnabled));
        }

        return new ReplayUserAccountsResult(accounts.Count);
    }
}
