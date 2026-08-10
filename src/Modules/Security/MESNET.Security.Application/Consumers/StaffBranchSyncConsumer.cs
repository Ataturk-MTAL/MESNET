using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Shared.Events;
using MESNET.Security.Application.Handlers;
using MESNET.Security.Application.Services;
using MESNET.Security.Core.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace MESNET.Security.Application.Consumers;

/// <summary>
/// Kurum personel kaydındaki <b>kurum ve alan</b> bilgisini kullanıcı hesabına yansıtır
/// (#126, ADR-0003 adım 2.1).
///
/// <para><b>İkincil (geçiş) yoldur.</b> Birincil yol kayıt sırasında girilen
/// <c>CreateUser.BranchCodes</c>'tur. Bu tüketici, personel kaydı zaten branş taşıyan
/// ama kullanıcı kaydında alan bulunmayan durumları doldurur — mevcut kullanıcılar için.</para>
///
/// <para><b>Kiracı anahtarı da burada doldurulur.</b> <c>UserAccount.InstitutionId</c> kiracı
/// anahtarının otoritesidir (ADR-0003), ama mevcut kullanıcıların çoğunda boştur — kurum bilgisi
/// bugüne kadar token claim'inden okunuyordu. Token yolu kapatılmadan ÖNCE bu boşluk
/// doldurulmalı, yoksa mevcut kullanıcılar kapsamsız kalıp kilitlenir.</para>
///
/// <para><b>Uydurma yok, üzerine yazma yok</b> — karar
/// <see cref="StaffAccountBackfillPolicy"/> içinde ve testle kilitli.</para>
///
/// <para><b>Branşın boş olması kurum backfill'ini ENGELLEMEZ.</b> Eskiden branş yoksa erken
/// dönülüyordu; bu, okul müdürü ve müdür yardımcısının kiracı anahtarını sessizce doldurulmamış
/// bırakırdı — ve onlar tam da hiçbir alana bağlı olmayan rollerdir.</para>
/// </summary>
public static class StaffBranchSyncConsumer
{
    public static async Task Consume(
        StaffAuthorized @event,
        IDocumentSession session,
        IKeycloakAdminService keycloak,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(@event.KeycloakId))
            return;

        var account = await session.Query<UserAccount>()
            .FirstOrDefaultAsync(u => u.KeycloakUserId == @event.KeycloakId && u.DeletedAt == null, cancellationToken);

        if (account is null)
            return;

        var changed = false;

        // ── Kiracı anahtarı (ADR-0003 adım 2.1) ──
        if (StaffAccountBackfillPolicy.ShouldFillInstitution(@event.InstitutionId, account.InstitutionId))
        {
            account.InstitutionId = @event.InstitutionId;
            changed = true;
        }

        // ── Alan (branş) kapsamı (#126) ──
        if (StaffAccountBackfillPolicy.ShouldFillBranches(@event.BranchCode, account.BranchCodes))
        {
            var branchCodes = CreateUserHandler.NormalizeBranchCodes([@event.BranchCode!]);
            if (branchCodes.Count > 0)
            {
                await keycloak.SetUserAttributeValuesAsync(
                    account.KeycloakUserId, BranchCodeClaims.ClaimType, branchCodes, cancellationToken);

                account.BranchCodes = branchCodes;
                changed = true;
            }
        }

        if (!changed) return;

        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);
    }
}
