using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using MESNET.Common.Shared.Tenancy;
using MESNET.Security.Core.Entities;

namespace MESNET.Security.Application.Services;

/// <summary>
/// Marten'dan UserAccount okuyarak güncel permission bilgisini sağlar.
/// PermissionClaimsTransformation tarafından her istekte kullanılır (5dk cache'li).
///
/// <para><b>Neden DI'dan gelen <c>IQuerySession</c> KULLANILMAZ (#149):</b> bu sağlayıcı
/// yetkilendirme <i>kurulurken</i>, yani istek kiracısı daha çözülmeden çalışır. Kiracılık
/// açıldıktan sonra DI'dan gelen session kiracısızdır ve
/// <c>DefaultTenantUsageDisabledException</c> fırlatır. Çağıran taraf o istisnayı yakalayıp
/// <b>token'daki rollere düşer</b> — ADR-0003 adım 2'nin kapattığı yolun ta kendisi. Ölçüldü:
/// kapı açıkken devre dışı bırakılmış bir hesap 22 izinle veri okumaya devam ediyordu.</para>
///
/// <para><b>Neden <see cref="TenantResolution.Platform"/>:</b> <c>UserAccount</c> kimlik
/// katmanıdır ve <see cref="DocumentTenancyMap"/> içinde <b>paylaşımlı</b> sınıflandırılmıştır —
/// satırlarında kiracı damgası yoktur, dolayısıyla hangi kiracıyla açıldığı sonucu
/// değiştirmez. Yine de bir ada ihtiyaç var, çünkü kiracısız session yasaktır; "hiçbir okula
/// ait olmayan iş" için doğru ad <c>platform</c>'dur.</para>
///
/// <para><b>Kırılganlık ve kilidi:</b> <c>UserAccount</c> ileride <c>Tenant</c> olarak
/// sınıflandırılırsa bu sorgu satır döndürmemeye başlar ve hata vermez — sessizce herkesi
/// yetkisiz bırakır. Sınıflandırma <c>UserAccountTenancyTests</c> ile kilitlidir.</para>
/// </summary>
public sealed class UserPermissionProvider : IUserPermissionProvider
{
    private readonly IDocumentStore _store;

    public UserPermissionProvider(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<UserPermissionInfo?> GetUserPermissionInfoAsync(string keycloakUserId)
    {
        await using var session = _store.QuerySession(TenantResolution.Platform);

        var account = await session.Query<UserAccount>()
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
            account.BusinessId,
            account.StudentId,
            account.LinkedStudentIds.AsReadOnly(),
            // Kaydın son yazılma anı (#208) — hiç güncellenmemişse oluşturulma anı.
            account.UpdatedAt ?? account.CreatedAt);
    }
}
