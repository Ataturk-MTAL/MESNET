using Marten;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Bilerek birden çok okulu okuyan sorgular için — okul başına bir session (#317).
///
/// <para><b>Neden <c>TenantIsOneOf</c> DEĞİL:</b> row-level security politikası yalnız
/// session'ın kiracısını gösterir. <c>TenantIsOneOf(...)</c> SQL'e <c>tenant_id = ANY(...)</c>
/// ekler ama politika o listeyi bilmez: il yöneticisinin takılan onay listesi yalnız kendi
/// kiracısını, platform sondası HİÇBİR şeyi görürdü — hata değil, eksik sonuç.</para>
///
/// <para><b>Neden BYPASSRLS rolü DEĞİL:</b> RLS'i atlayan ayrı bir veri kaynağı, yalıtımı
/// veritabanında delen tek kalıcı yol olurdu. Okul başına session ile politika bu yolda da
/// yürürlükte kalır. Bedeli kapsamdaki okul sayısı kadar sorgudur; kullanan yollar yönetim
/// ekranı ve dağıtım sondasıdır, sıcak yol değildir.</para>
///
/// <para>Kiracı listesi çağırandan gelir ve istekten ALINMAZ — <c>SubtreeTenantScope</c> ya da
/// <c>ITenantDirectory</c>.</para>
/// </summary>
public static class CrossTenantQuery
{
    /// <param name="perTenant">Tek okulun session'ında çalışan sorgu; kiracı süzgeci EKLEMEZ.</param>
    /// <param name="limit">Toplam üst sınır; aşılınca kalan okullar sorgulanmaz.</param>
    public static async Task<List<T>> CollectAsync<T>(
        IDocumentStore store,
        IEnumerable<string> tenantIds,
        Func<IQuerySession, int, CancellationToken, Task<IReadOnlyList<T>>> perTenant,
        CancellationToken cancellationToken,
        int limit = int.MaxValue)
    {
        var result = new List<T>();

        foreach (var tenantId in tenantIds.Distinct(StringComparer.Ordinal))
        {
            var remaining = limit - result.Count;
            if (remaining <= 0)
                break;

            await using var session = store.QuerySession(tenantId);
            result.AddRange(await perTenant(session, remaining, cancellationToken));
        }

        return result;
    }
}
