using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Core.Services;

/// <summary>
/// Kurum koordinasyon yapılandırmasında kırılan tek kural (#134).
///
/// <para>Yalnız "hangi kural" değil "hangi değer" bilgisini de taşır — kullanıcı tabloda
/// düzeltmesi gereken satırı görür, "bir yerde hata var" mesajı almaz.</para>
/// </summary>
/// <param name="Kind">Kırılan kural.</param>
/// <param name="DistanceKm">Kuralı kıran mesafe sınırı — mesafeye bağlı olmayan kurallarda <c>null</c>.</param>
/// <param name="Hours">Kuralı kıran saat değeri — saate bağlı olmayan kurallarda <c>null</c>.</param>
public sealed record CoordinationConfigViolation(
    CoordinationConfigViolationKind Kind,
    double? DistanceKm = null,
    int? Hours = null);
