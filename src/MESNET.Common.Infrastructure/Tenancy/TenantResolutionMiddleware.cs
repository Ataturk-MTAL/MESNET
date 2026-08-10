using System.Security.Claims;
using MESNET.Common.Shared.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// İstek başına kiracıyı çözer ve <see cref="IMessageBus.TenantId"/> üzerine koyar (#149).
///
/// <para><b>Neden middleware, uçlarda tek tek değil:</b> uygulamada 219 <c>bus.InvokeAsync</c>
/// çağrısı var. Kiracıyı çağrı yerinde vermek, <b>her yeni ucun</b> onu hatırlamasını
/// gerektirirdi; unutulan tek bir uç, verinin sessizce yanlış bölmeye gitmesi demektir.
/// Burada bir kez konunca handler'lar, cascading mesajlar ve <c>PublishAsync</c> çağrıları
/// kiracıyı <b>devralır</b>.</para>
///
/// <para><b>Wolverine düz Minimal API'de kiracıyı otomatik tespit etmez.</b> Otomatik tespit
/// yalnız <c>Wolverine.Http</c> uçları içindir; bu uygulamanın uçları ASP.NET Minimal API'dir.
/// <c>IMessageBus</c> <i>scoped</i> olduğu için burada yazılan değer, aynı isteğin uçlarında
/// çözülen örnekle aynıdır.</para>
///
/// <para><b>Kiracı UYDURULMAZ.</b> Çözülemezse hiçbir şey yazılmaz; kiracıya ait veriye erişim
/// gürültülü biçimde başarısız olur. Sessizce varsayılan kiracıya düşmek, kiracılığın
/// engellemek için var olduğu hatanın kılık değiştirmiş hâlidir.</para>
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Kimliği doğrulanmamış istek (health, metrics, OpenAPI) kiracıya ait veriye dokunmaz.
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantId = TenantResolution.Resolve(
                InstitutionIdOf(context.User),
                context.User.FindAll("permissions").Select(c => c.Value));

            if (tenantId is not null)
            {
                context.RequestServices.GetRequiredService<IMessageBus>().TenantId = tenantId;
            }
            else
            {
                // Debug: kapsamsızlık zaten izin dönüşümünde bildiriliyor (ADR-0003 adım 2);
                // burada her istekte tekrar uyarmak o sinyali gürültüye boğardı.
                logger.LogDebug(
                    "İstek kiracısız çalışıyor: {Path}. Kullanıcının kurum kapsamı yok.",
                    context.Request.Path);
            }
        }

        await next(context);
    }

    private static Guid? InstitutionIdOf(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst("institution_id")?.Value, out var id) ? id : null;
}
