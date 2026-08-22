using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

/// <summary>
/// Kurum koordinasyon yapılandırmasında (mesafe-saat tablosu, azami haftalık ek ders
/// saati) kırılan kuralın türü (#134).
///
/// <para><see cref="SmartEnum{T}.Name"/> değerleri <c>CoordinationErrors</c> içindeki hata
/// kodlarının son ekiyle birebir aynıdır (<c>Coordination.{Name}</c>) — frontend tek bir
/// eşleme tablosu tutar. Aynı kural <see cref="HoursViolationKind"/> için de geçerlidir.</para>
/// </summary>
public sealed class CoordinationConfigViolationKind : SmartEnum<CoordinationConfigViolationKind>
{
    /// <summary>Mesafe-saat tablosu gönderildi ama boş.</summary>
    public static readonly CoordinationConfigViolationKind EmptyDistanceHourRules =
        new(nameof(EmptyDistanceHourRules), 1, "boş mesafe-saat tablosu");

    /// <summary>Bir kuralın mesafe sınırı 0 veya negatif.</summary>
    public static readonly CoordinationConfigViolationKind InvalidDistanceHourRuleDistance =
        new(nameof(InvalidDistanceHourRuleDistance), 2, "geçersiz mesafe sınırı");

    /// <summary>Bir kuralın saati izin verilen aralığın dışında.</summary>
    public static readonly CoordinationConfigViolationKind InvalidDistanceHourRuleHours =
        new(nameof(InvalidDistanceHourRuleHours), 3, "geçersiz koordinatörlük saati");

    /// <summary>Aynı mesafe sınırı tabloda birden çok kez geçiyor.</summary>
    public static readonly CoordinationConfigViolationKind DuplicateDistanceHourRule =
        new(nameof(DuplicateDistanceHourRule), 4, "yinelenen mesafe sınırı");

    /// <summary>Tabloda "üzeri (sınırsız)" catch-all kuralı yok.</summary>
    public static readonly CoordinationConfigViolationKind MissingUnlimitedDistanceHourRule =
        new(nameof(MissingUnlimitedDistanceHourRule), 5, "eksik sınırsız kural");

    /// <summary>Azami haftalık ek ders saati izin verilen aralığın dışında.</summary>
    public static readonly CoordinationConfigViolationKind InvalidMaxWeeklyExtraHours =
        new(nameof(InvalidMaxWeeklyExtraHours), 6, "geçersiz azami haftalık ek ders saati");

    private CoordinationConfigViolationKind(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    /// <summary>Kuralın Türkçe adı — hata mesajında "hangi kural" bilgisini taşır.</summary>
    public string Slug { get; }
}
