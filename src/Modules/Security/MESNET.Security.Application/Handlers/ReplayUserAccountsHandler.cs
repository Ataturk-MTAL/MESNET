using Marten;
using MESNET.Security.Application.Commands;
using MESNET.Security.Core.Entities;
using MESNET.Security.Shared.Events;
using Wolverine;

namespace MESNET.Security.Application.Handlers;

/// <summary>
/// <see cref="ReplayUserAccounts"/> sonucu — kaç hesap yeniden yayınlandı, kaçı ek olarak pasif
/// durumuyla işaretlendi (operatör görsün diye ikisi ayrı sayılır).
/// </summary>
public sealed record ReplayUserAccountsResult(int Replayed, int MarkedDeactivated);

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

        var markedDeactivated = 0;

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

            // UserCreated tüketicisi (Task 6, InstitutionManagerLinkConsumer) IsEnabled'ı
            // KOŞULSUZ true yazar — olay şeması etkinlik durumu taşımaz ve başka modüllerin
            // tüketicileriyle PAYLAŞILAN bir sözleşmedir, burada genişletilmez. Hesap zaten
            // pasifse ikinci bir olay yayınlanır; InstitutionManagerLinkConsumer'ın kuyruğu
            // Sequential() olduğu için aynı kullanıcının olayları normal yolda YAYIN SIRASIYLA
            // işlenir ve bu ikinci olay IsEnabled'ı doğru değere (false) döndürür. Aksi hâlde
            // pasif bir yöneticinin hesabı "etkin yönetici" sayılır, okulu yöneticisiz
            // listesinden SESSİZCE düşürür — bu ucun tüm amacı tam da bunu önlemektir.
            //
            // Sıra GARANTİ DEĞİLDİR: Sequential() yalnız paralellik derecesini 1'e indirir,
            // sıralı YENİDEN teslimatı taahhüt etmez. UserCreated başarısız olup yeniden
            // denenirken UserDeactivated önce başarıyla işlenmişse, ya da replay ortasında
            // süreç yeniden başlarsa sıra TERS DÖNEBİLİR — belirti aynıdır: bağlantı sessizce
            // yeniden "etkin" görünür. Onarım budur: bu uç idempotenttir, yeniden çalıştırmak
            // durumu düzeltir.
            if (!account.IsEnabled)
            {
                await bus.PublishAsync(new UserDeactivated(
                    account.Id,
                    account.KeycloakUserId,
                    "Yeniden yayın (replay) — hesap zaten pasifti, durum eski hâline döndürüldü."));

                markedDeactivated++;
            }
        }

        return new ReplayUserAccountsResult(accounts.Count, markedDeactivated);
    }
}
