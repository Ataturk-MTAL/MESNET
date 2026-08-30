using Ardalis.SmartEnum;

namespace MESNET.Audit.Core.Enums;

/// <summary>
/// Bir komutun denetim izindeki sonucu.
/// </summary>
/// <remarks>
/// <para><b>Üç değer, iki farklı soru:</b> <see cref="Rejected"/> "sistem çalıştı, kural
/// izin vermedi" der; <see cref="Failed"/> "sistem çalışmadı" der. Denetim okuyucusu için
/// bu ayrım load-bearing'dir: ilki bir davranış kaydı, ikincisi bir arıza kaydıdır.</para>
///
/// <para><b>Yetki reddi (403) burada YOKTUR</b> ve olamaz — ASP.NET yetkilendirme katmanı
/// isteği handler'dan önce keser, denetim middleware'i hiç çalışmaz. İzdeki
/// <see cref="Rejected"/> satırları yalnız <c>DomainException</c> kaynaklıdır (kurum kapsamı
/// ihlali dahil: o guard middleware'de çalışır ve yakalanır).</para>
/// </remarks>
public sealed class AuditOutcome : SmartEnum<AuditOutcome>
{
    public static readonly AuditOutcome Succeeded = new(nameof(Succeeded), 1, "Başarılı");
    public static readonly AuditOutcome Rejected = new(nameof(Rejected), 2, "Reddedildi");
    public static readonly AuditOutcome Failed = new(nameof(Failed), 3, "Hata");

    /// <summary>Türkçe arayüz etiketi.</summary>
    public string Slug { get; }

    private AuditOutcome(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    /// <summary>
    /// Saklanan düz metinden tipi çözer. <b>Tanınmayan ya da boş değer
    /// <see cref="Failed"/>'a düşer</b>: bilinmeyen bir sonucu "başarılı" saymak, denetim
    /// izinde en zararlı varsayılan olurdu.
    /// </summary>
    public static AuditOutcome Resolve(string? name)
        => !string.IsNullOrWhiteSpace(name) && TryFromName(name, out var outcome)
            ? outcome
            : Failed;
}
