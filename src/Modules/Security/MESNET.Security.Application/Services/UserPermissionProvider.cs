using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using MESNET.Security.Core.Entities;

namespace MESNET.Security.Application.Services;

/// <summary>
/// Marten'dan UserAccount okuyarak güncel permission bilgisini sağlar.
/// PermissionClaimsTransformation tarafından her istekte kullanılır (5dk cache'li).
/// </summary>
public sealed class UserPermissionProvider : IUserPermissionProvider
{
    private readonly IQuerySession _session;

    public UserPermissionProvider(IQuerySession session)
    {
        _session = session;
    }

    public async Task<UserPermissionInfo?> GetUserPermissionInfoAsync(string keycloakUserId)
    {
        var account = await _session.Query<UserAccount>()
            .Where(u => u.KeycloakUserId == keycloakUserId)
            .FirstOrDefaultAsync();

        if (account is null)
            return null;

        return new UserPermissionInfo(
            // Silinmiş kayıt de erişim üretmez (#210). Bu sorgu mezar taşlarını BİLEREK
            // süzmez — kaydı bulamazsak dönüşüm token yedeğine düşer ve izinler token'daki
            // rollerden yeniden türetilir; tam kaçındığımız durum budur.
            UserAccountAccessPolicy.GrantsAccess(account.IsEnabled, account.DeletedAt),
            account.Roles.AsReadOnly(),
            account.DirectPermissions.AsReadOnly(),
            account.BranchCodes.AsReadOnly(),
            account.InstitutionId,
            account.LinkedStudentIds.AsReadOnly(),
            // Kaydın son yazılma anı (#208) — hiç güncellenmemişse oluşturulma anı.
            account.UpdatedAt ?? account.CreatedAt);
    }
}
