namespace MESNET.Business.Core.ValueObjects;

/// <summary>
/// İşletmenin belirli bir alandan (branş) öğrenci alma yetkisi (#119).
///
/// Yetki idari bir karardır: işletme belgelerini yükler, idare belgeleri inceler ve hangi
/// alanlardan öğrenci alabileceğini işaretler. Dayanak belge <see cref="BasedOnDocumentId"/>
/// ile kayda bağlanır.
///
/// Yetki KALDIRILDIĞINDA kayıt silinmez — <see cref="RevokedAt"/> damgalanır. Böylece denetim
/// izi korunur ve "dönem içi kazanılmış hak" (mevcut yerleştirmeler) bozulmaz; yalnız YENİ
/// yerleştirme reddedilir.
/// </summary>
public sealed record BranchAuthorization
{
    /// <summary>Alan kodu — EET, BT, MTT ... (Institution modülündeki FieldCode ile aynı).</summary>
    public required string BranchCode { get; init; }

    /// <summary>Yetkinin dayandığı belge (işletmenin Documents listesinden). Geçiş dolgusunda null.</summary>
    public Guid? BasedOnDocumentId { get; init; }

    public DateTime AuthorizedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Yetkiyi veren idareci (ad soyad).</summary>
    public required string AuthorizedBy { get; init; }

    /// <summary>Yetki kaldırıldıysa kaldırılma zamanı; null ise yetki aktiftir.</summary>
    public DateTime? RevokedAt { get; init; }

    /// <summary>Aktif yetki = iptal edilmemiş yetki.</summary>
    public bool IsActive => RevokedAt is null;
}
