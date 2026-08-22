using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace MESNET.Common.Infrastructure.Logging;

/// <summary>
/// Her log kaydına kiracı (kurum) kimliğini ekler.
///
/// <para><b>Neden bugünden, tek kurumlu hâldeyken:</b> log şeması en hızlı taşlaşan
/// yüzeydir. Alan olmadan kurulan her pano, uyarı ve saklama kuralı, çok kurumluya
/// geçerken yeniden yazılır. Alanı bugün koymak saatlik iş; sonra koymak tüm
/// gözlemlenebilirlik yapılandırmasının elden geçirilmesi demektir.</para>
///
/// <para><b>Neden alan, ayrı stream/organization değil:</b> tek süreç tek OTLP
/// dışa aktarıcısıyla çalışır; kayıtları N akışa dağıtmak ya N dışa aktarıcı ya da bir
/// yönlendirme katmanı ister. Alan, seçeneği açık tutan hamledir — gerekirse OpenObserve
/// tarafında bu alana bakan bir pipeline ile akışlara ayrılır, uygulama hiç değişmez.</para>
///
/// <para><b>Bu bir yetkilendirme sınırı DEĞİLDİR.</b> Alan bazlı ayrım filtrelemedir,
/// güvenlik değil; OpenObserve'un yetki modeli organization/stream'e bağlıdır. Bir okulun
/// yöneticisine yalnız kendi loglarını göstermek gerekirse alan yeterli olmaz.</para>
/// </summary>
public sealed class TenantEnricher : ILogEventEnricher
{
    /// <summary>Log kaydındaki alan adı. Panolar ve uyarılar bu ada bağlanır.</summary>
    public const string PropertyName = "TenantId";

    /// <summary>
    /// HTTP bağlamı olmayan kayıtlar (arka plan işleri, Marten async daemon, başlangıç
    /// logları). Boş bırakmak yerine açık bir değer: "kiracısı yok" ile "kiracısı
    /// belirlenemedi" ayrımı sorguda kaybolmasın.
    /// </summary>
    public const string SystemTenant = "system";

    /// <summary>Kimliği doğrulanmış ama kuruma bağlı olmayan kullanıcı (ör. sistem yöneticisi).</summary>
    public const string UnknownTenant = "unknown";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(
            propertyFactory.CreateProperty(PropertyName, ResolveTenant()));
    }

    /// <summary>
    /// Kiracı, kimliği doğrulanmış kullanıcının <c>institution_id</c> claim'inden okunur.
    ///
    /// <para>O claim <c>PermissionClaimsTransformation</c> tarafından <b>kullanıcı kaydından</b>
    /// yazılır; token'dan gelen değer kayıt doluysa atılır. Yani buradaki değer kullanıcının
    /// kendi belirleyebileceği bir kaynaktan gelmez — kiracı anahtarı olarak kullanılabilmesinin
    /// ön şartı budur.</para>
    /// </summary>
    private string ResolveTenant()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            return SystemTenant;

        var institutionId = user.FindFirst("institution_id")?.Value;

        return string.IsNullOrWhiteSpace(institutionId) ? UnknownTenant : institutionId;
    }
}
