using System.Text.Json;

namespace MESNET.Seeder.Seeders;

/// <summary>
/// Kurumun hangi il/ilçe alanının tamamlanması gerektiği (#196). Saf fonksiyon — G/Ç yapmaz.
/// </summary>
/// <remarks>
/// <para><b>Kilitlenen hata:</b> koruma yalnız <c>provinceCode</c>'a bakıp doluysa erken
/// dönüyordu, oysa PATCH gövdesi il ve ilçeyi <b>birlikte</b> yazıyordu. Il bir kez dolduktan
/// sonra ilçe kalıcı olarak boş kalıyor ve hiçbir koşu onu tamamlamıyordu. Canlı veride tam
/// olarak bu olmuştu: il <c>33</c>, ilçe <c>null</c>.</para>
///
/// <para>Doğrulama da yakalamaz — ilçe yalnız <i>doluysa</i> doğrulanır, boş bırakmak geçerlidir.
/// Bu yüzden karar burada adlandırıldı ve testle kilitlendi.</para>
/// </remarks>
public static class InstitutionBackfillPolicy
{
    /// <summary>Hangi alanların doldurulması gerektiği. İkisi de <c>false</c> ise yapılacak iş yok.</summary>
    public static (bool Province, bool District) MissingFields(JsonElement institution)
        => (IsBlank(institution, "provinceCode"), IsBlank(institution, "districtName"));

    /// <summary>Alan yok, null ya da yalnız boşluk mu?</summary>
    public static bool IsBlank(JsonElement element, string propertyName)
        => !element.TryGetProperty(propertyName, out var value)
           || value.ValueKind != JsonValueKind.String
           || string.IsNullOrWhiteSpace(value.GetString());
}
