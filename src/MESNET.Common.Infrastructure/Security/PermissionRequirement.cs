using Microsoft.AspNetCore.Authorization;

namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// Bir ya da <b>birkaç</b> izinden herhangi birini isteyen policy gereksinimi.
///
/// <para><b>Neden çoklu (#182):</b> bazı listeleme uçları hem okul tarafına hem veri sahibine
/// açıktır — devamsızlık listesini okul <c>attendance:view</c> ile, öğrenci ve veli
/// <c>attendance:view-own</c> ile görür. İkisi ayrı uç olsaydı aynı sorgu iki kez yazılır ve
/// kapsam kuralı iki yerde tutulurdu; tek uç + kapsam merdiveni tercih edildi.</para>
///
/// <para><b>Erişim ile kapsam ayrıdır</b> (ADR-0001): bu gereksinim yalnız ucun açılıp
/// açılmayacağına karar verir. "Hangi verinin" sorusunu handler'daki kapsam merdiveni
/// cevaplar — <c>view-own</c> ile giren kullanıcı yalnız kendi kapsamını görür.</para>
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; }

    public PermissionRequirement(params string[] permissions)
    {
        if (permissions.Length == 0)
            throw new ArgumentException("En az bir izin gerekir.", nameof(permissions));

        Permissions = permissions;
    }
}
