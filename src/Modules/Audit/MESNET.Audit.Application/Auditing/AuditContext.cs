using System.Diagnostics;

namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// Middleware'in <c>Before</c> → <c>After</c> → <c>Finally</c> hook'ları arasında taşıdığı
/// durum.
/// </summary>
/// <remarks>
/// <para><b>Neden MUTABLE — projede tek istisna:</b> Wolverine'in ürettiği kodda
/// <c>After</c>, başarı yolunda <c>try</c> bloğunun içinde çalışır ve <c>Finally</c> ondan
/// sonra gelir. Bir <i>değer</i> döndürüp aktarmak mümkün değildir: <c>try</c> içinde atanan
/// bir değişken <c>finally</c> bloğunda "kesin atanmış" sayılmaz ve derleme kırılır. Tek
/// mutasyon <see cref="Succeeded"/>'dır ve yalnız <see cref="MarkSucceeded"/> ile yapılır.</para>
///
/// <para><b>Varsayılan başarısızdır.</b> <c>After</c> hiç çalışmazsa (istisna yolu)
/// bayrak <c>false</c> kalır ve <c>Finally</c> hiçbir şey yazmaz — o satırı
/// <c>OnExceptionAsync</c> yazar.</para>
/// </remarks>
public sealed class AuditContext
{
    /// <summary>
    /// Denetim satırının kimliği — <b>bilerek TEK sefer üretilir ve komutun ömrü boyunca
    /// sabit kalır.</b>
    /// </summary>
    /// <remarks>
    /// Handler döndükten SONRA (transaction commit / cascading publish) patlayan bir hata
    /// senaryosunda <c>Finally</c> önce "Succeeded" satırını yazar, sonra <c>OnException</c>
    /// "Failed" satırını yazar (hook sırası: <c>Finally</c> → <c>OnException</c>). İkisi de
    /// AYNI <see cref="AuditContext"/> örneğini kullanır — yani aynı <see cref="Id"/>'yi.
    /// <see cref="AuditWriter"/> bu kimlikle Marten <c>Store()</c> çağırır ve Marten
    /// <c>Store()</c> varsayılan olarak kimliğe göre UPSERT yapar (<c>AuditMartenConfig</c>'te
    /// tersini söyleyen bir <c>.Identity()</c> yapılandırması yok). Sonuç: ikinci yazma
    /// birincinin ÜSTÜNE yazar ve iz kalıcı olarak "Failed" görünür — geri alınmış bir komut
    /// hiçbir zaman "Succeeded" olarak takılı kalmaz. Kimlik yazma başına üretilseydi aynı
    /// komut için İKİ ayrı satır (biri yanlışlıkla "başarılı") doğardı.
    /// </remarks>
    public Guid Id { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;

    private readonly long _startTimestamp = Stopwatch.GetTimestamp();

    public required Guid ActorId { get; init; }
    public required string ActorName { get; init; }
    public required Type CommandType { get; init; }
    public required object? Command { get; init; }
    public required string? TenantId { get; init; }
    public required Guid? ActorInstitutionId { get; init; }
    public required string? ActorInstitutionPath { get; init; }

    public bool Succeeded { get; private set; }

    public void MarkSucceeded() => Succeeded = true;

    public int ElapsedMs => (int)Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;
}
