using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Institution.Shared.Events;
using MESNET.Security.Application.Handlers;
using MESNET.Security.Application.Services;
using MESNET.Security.Core.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace MESNET.Security.Application.Consumers;

/// <summary>
/// Kurum personel kaydındaki alan (branş) bilgisini kullanıcı hesabına yansıtır (#126).
///
/// <para><b>İkincil (geçiş) yoldur.</b> Birincil yol kayıt sırasında girilen
/// <c>CreateUser.BranchCodes</c>'tur. Bu tüketici, personel kaydı zaten branş taşıyan
/// ama kullanıcı kaydında alan bulunmayan durumları doldurur — mevcut kullanıcılar için.</para>
///
/// <para><b>Uydurma yok, üzerine yazma yok:</b></para>
/// <list type="bullet">
///   <item>Olayda branş yoksa hiçbir şey yapılmaz (müdür/müdür yrd. — normal durum)</item>
///   <item>Kullanıcı kaydında zaten alan varsa DOKUNULMAZ — idarenin elle girdiği
///         kapsam, personel kaydından gelen tahminle ezilmez</item>
/// </list>
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
        if (string.IsNullOrWhiteSpace(@event.BranchCode) || string.IsNullOrWhiteSpace(@event.KeycloakId))
            return;

        var account = await session.Query<UserAccount>()
            .FirstOrDefaultAsync(u => u.KeycloakUserId == @event.KeycloakId && u.DeletedAt == null, cancellationToken);

        if (account is null)
            return;

        // Elle girilmiş kapsam korunur — bu tüketici yalnız BOŞLUĞU doldurur.
        if (account.BranchCodes.Count > 0)
            return;

        var branchCodes = CreateUserHandler.NormalizeBranchCodes([@event.BranchCode]);
        if (branchCodes.Count == 0)
            return;

        await keycloak.SetUserAttributeValuesAsync(
            account.KeycloakUserId, BranchCodeClaims.ClaimType, branchCodes, cancellationToken);

        account.BranchCodes = branchCodes;
        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);
    }
}
