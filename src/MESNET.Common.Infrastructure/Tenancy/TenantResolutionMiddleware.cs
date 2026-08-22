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
        // ── Kimliği doğrulanmamış istek → kimlik katmanı kiracısı ────────────────────────
        // Kimliği olmayan çağıran hiçbir okula ait olamaz. Çoğu için (health, metrics,
        // OpenAPI) bunun sonucu yok; ama uygulamada bir tane gerçek anonim uç var —
        // davet tamamlama — ve o bir Wolverine handler'ı çalıştırır. Kiracı verilmezse
        // istisna belge erişiminde DEĞİL, session AÇILIRKEN atılır: handler daha ilk satırını
        // çalıştırmadan 500 döner. Ölçüldü: anonim
        // POST /api/security/invitations/{id}/complete → DefaultTenantUsageDisabledException.
        //
        // Bu "kiracı uydurmak" değildir: anonim çağıranın dokunabildiği belgeler kimlik
        // katmanındadır (UserInvitation, UserAccount) ve kiracı damgası TAŞIMAZLAR — hangi
        // kiracıyla okundukları sonucu değiştirmez. Kiracıya ait bir belgeye dokunan yeni bir
        // anonim uç, kapsamsız kalmak yerine boş sonuç görürdü; bu yüzden anonim uçların
        // listesi AnonymousEndpointDriftTests ile kilitlidir.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.RequestServices.GetRequiredService<IMessageBus>().TenantId =
                TenantResolution.Platform;

            await next(context);
            return;
        }

        var tenantId = TenantResolution.Resolve(
            InstitutionIdOf(context.User),
            context.User.FindAll("permissions").Select(c => c.Value));

        if (tenantId is not null)
        {
            context.RequestServices.GetRequiredService<IMessageBus>().TenantId = tenantId;
        }
        else
        {
            // Kimliği DOĞRULANMIŞ ama kapsamsız kullanıcı: burada platform kiracısına
            // düşülmez. Anonim çağıranın aksine bu kullanıcı okul verisi yazmaya çalışır ve
            // platforma yazması, veriyi sessizce yanlış bölmeye göndermek olurdu. Kiracısız
            // kalır; erişim gürültülü biçimde başarısız olur.
            //
            // Debug: kapsamsızlık zaten izin dönüşümünde bildiriliyor (ADR-0003 adım 2);
            // burada her istekte tekrar uyarmak o sinyali gürültüye boğardı.
            logger.LogDebug(
                "İstek kiracısız çalışıyor: {Path}. Kullanıcının kurum kapsamı yok.",
                context.Request.Path);
        }

        await next(context);
    }

    private static Guid? InstitutionIdOf(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst("institution_id")?.Value, out var id) ? id : null;
}
