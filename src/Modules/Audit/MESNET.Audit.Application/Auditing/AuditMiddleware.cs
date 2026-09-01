using System.Runtime.ExceptionServices;
using MESNET.Common.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// Her yazma komutunu denetim izine kaydeden GENEL Wolverine middleware'i.
/// </summary>
/// <remarks>
/// <para><b>Hook şekli ÖLÇÜLDÜ (Wolverine 6.15.0, 28.08.2026) — değiştirmeyin:</b></para>
/// <list type="number">
/// <item><c>Before</c> → [handler] → <c>After</c> (yalnız başarıda) → <c>Finally</c>
/// (her zaman) → <c>OnException</c> (yalnız istisnada, <c>Finally</c>'den SONRA).</item>
/// <item><c>OnException</c>, <c>Before</c>'un DÖNDÜRDÜĞÜ değeri göremez —
/// <c>CS0103: The name 'ctx' does not exist in the current context</c>. Yalnız
/// <c>Exception</c>, <see cref="Envelope"/> ve DI servisleri alabilir.</item>
/// <item><b><c>OnException</c> istisnayı YUTAR.</b> Rethrow edilmezse çağıran hiçbir istisna
/// görmez; <c>DomainException</c> kaybolur, HTTP 422 doğmaz ve <b>reddedilen komut başarılı
/// görünür</b>. <see cref="ExceptionDispatchInfo"/> ile rethrow ZORUNLUDUR ve
/// <c>AuditMiddlewareContractTests</c> ile kilitlidir.</item>
/// </list>
///
/// <para><b>Neden statik sınıf ama <c>AddMiddleware&lt;T&gt;</c> DEĞİL:</b> tip parametreli
/// aşırı yükleme statik sınıf kabul etmez (<c>CS0718</c>). Kayıt
/// <c>opts.Policies.AddMiddleware(typeof(AuditMiddleware), filter)</c> ile yapılır.</para>
///
/// <para><b>Yetki reddi (403) buraya ULAŞMAZ</b> — ASP.NET yetkilendirme katmanı isteği
/// handler'dan önce keser. Bilinen ve kabul edilen bedeldir.</para>
/// </summary>
public static class AuditMiddleware
{
    /// <summary>
    /// Bağlamı kurar, <see cref="AuditContextAccessor"/>'e koyar ve döndürür.
    /// </summary>
    /// <remarks>
    /// <b>Hem döndürülür hem accessor'a konur</b> ve bu bir tekrar değildir: döndürülen değer
    /// <c>After</c>/<c>Finally</c>'nin parametresi olur (Wolverine değişken zincirlemesi),
    /// accessor ise <c>OnExceptionAsync</c>'in TEK erişim yoludur — catch bloğu try'dan önce
    /// üretilen değişkenleri göremez (ölçüldü, <c>CS0103</c>).
    /// </remarks>
    public static AuditContext Before(
        Envelope envelope, ICurrentUserService currentUser, AuditContextAccessor accessor)
    {
        var actor = currentUser.GetCurrentUser();

        var context = new AuditContext
        {
            ActorId = actor?.UserId ?? Guid.Empty,
            // Denormalize: kullanıcı kaydı silinse bile iz okunur kalmalı, ayrıca okuma
            // anında ad çözmek modüller arası sorgu demektir ve yasaktır.
            ActorName = actor?.FullName ?? string.Empty,
            CommandType = envelope.Message?.GetType() ?? typeof(object),
            Command = envelope.Message,
            TenantId = envelope.TenantId,
            ActorInstitutionId = actor?.InstitutionId,
            ActorInstitutionPath = currentUser.GetInstitutionPath(),
            ActiveInstitutionId = actor?.ActiveInstitutionId,
        };

        accessor.Set(context);
        return context;
    }

    /// <summary>Yalnız başarı yolunda çalışır. Tek işi bayrağı kaldırmak.</summary>
    public static void After(AuditContext auditContext) => auditContext.MarkSucceeded();

    /// <summary>
    /// Her zaman çalışır ama <b>yalnız başarıda yazar</b>. Başarısızlık satırının sahibi
    /// <see cref="OnExceptionAsync"/>'dir; <c>Finally</c> istisnayı göremez.
    /// </summary>
    /// <remarks>
    /// <b>Yazıcı çağrısı try/catch İLE SARILIDIR.</b> <see cref="IAuditWriter"/>'ın ÜRETİM
    /// uygulaması (<c>AuditWriter</c>) kendi içinde her şeyi yakalar, ama sözleşme
    /// UYGULAMAYA bağımlı OLMAMALI: alternatif/test bir <c>IAuditWriter</c> fırlatırsa bu
    /// BAŞARILI komutun kendi sonucunu bozmamalı. İz en-iyi-çabadır (bkz.
    /// <c>AuditWriter</c> sınıf yorumu); burada da aynı ilke — denetim tablosundaki bir arıza
    /// BAŞARILI bir komutu 500'e çeviremez.
    /// </remarks>
    public static async Task FinallyAsync(
        AuditContext auditContext, IAuditWriter writer, ILogger<AuditWriter> logger,
        CancellationToken cancellationToken)
    {
        if (!auditContext.Succeeded) return;

        try
        {
            await writer.WriteAsync(auditContext, exception: null, cancellationToken);
        }
        catch (Exception ex)
        {
            // İZ EN-İYİ-ÇABADIR. Burada fırlatmak başarılı bir komutu 500'e çevirirdi.
            logger.LogError(ex,
                "Denetim satırı yazılamadı (başarı yolu) — Komut: {CommandType}, Aktör: {ActorId}",
                auditContext.CommandType.Name, auditContext.ActorId);
        }
    }

    /// <summary>
    /// Başarısızlık satırını yazar ve <b>istisnayı yeniden fırlatır</b>.
    /// </summary>
    /// <remarks>
    /// <b>Rethrow SİLİNEMEZ.</b> Silinirse Wolverine istisnayı yutar: <c>DomainException</c>
    /// HTTP katmanına hiç ulaşmaz, 422 yerine 200 döner ve reddedilen her komut başarılı
    /// görünür. Ölçüldü. Kilitleyen test: <c>AuditMiddlewareContractTests</c>.
    ///
    /// <para><b>Yazıcı çağrısı da try/catch İLE SARILIDIR ve rethrow bunun DIŞINDA
    /// kalır.</b> Sarılmazsa <see cref="IAuditWriter"/>'ın kendi istisnası aşağıdaki
    /// <c>ExceptionDispatchInfo</c> satırına hiç ULAŞILMADAN çağırana gider — orijinal
    /// <c>DomainException</c>'ın yerini denetim istisnası alır (422 yerine 500). Hata
    /// sözleşmesi <see cref="IAuditWriter"/> uygulamasına bağımlı OLMAMALI.</para>
    /// </remarks>
    public static async Task OnExceptionAsync(
        Exception exception,
        Envelope envelope,
        AuditContextAccessor accessor,
        IAuditWriter writer,
        ILogger<AuditWriter> logger,
        CancellationToken cancellationToken)
    {
        if (accessor.Current is { } auditContext)
        {
            try
            {
                await writer.WriteAsync(auditContext, exception, cancellationToken);
            }
            catch (Exception writeEx)
            {
                // İZ EN-İYİ-ÇABADIR. Yazıcının kendi hatası orijinal istisnanın YERİNİ
                // ALAMAZ — aşağıdaki rethrow her koşulda çalışmalı.
                logger.LogError(writeEx,
                    "Denetim satırı yazılamadı (ret/hata yolu) — Komut: {CommandType}, Aktör: {ActorId}",
                    auditContext.CommandType.Name, auditContext.ActorId);
            }
        }

        ExceptionDispatchInfo.Capture(exception).Throw();
    }
}
