using Marten;
using MESNET.Security.Application.Commands;
using MESNET.Security.Core.Entities;
using MESNET.Security.Shared.Events;
using Wolverine;

namespace MESNET.Security.Application.Handlers;

/// <summary>
/// <inheritdoc cref="ReplayUserAccounts"/>
/// </summary>
public static class ReplayUserAccountsHandler
{
    public static async Task<int> Handle(
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
            await bus.PublishAsync(new UserCreated(
                account.Id,
                account.KeycloakUserId,
                account.Username,
                account.FullName,
                account.Email,
                account.Roles,
                account.InstitutionId,
                account.BusinessId,
                new Dictionary<string, string>()));
        }

        return accounts.Count;
    }
}
