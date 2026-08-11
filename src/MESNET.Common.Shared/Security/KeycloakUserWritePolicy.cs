namespace MESNET.Common.Shared.Security;

/// <summary>
/// Keycloak kullanıcı temsiline yazarken hangi gövdenin güvenli olduğunu belirler
/// (ADR-0003 adım 3).
///
/// <para><b>Ölçülen kural (Keycloak 26.7.0):</b> PUT gövdesinde <c>attributes</c> varsa istek
/// <i>tam profil yazımı</i> sayılır — gövdede geçmeyen <c>firstName</c>/<c>email</c>
/// <b>silinir</b> ve öznitelik haritası tümüyle gönderilenle değişir. Anahtar yoksa istek kısmi
/// kalır ve diğer alanlara dokunulmaz. Aynı kullanıcıda arka arkaya ölçüldü:</para>
/// <code>
/// PUT {"enabled":false}                       → firstName=Deneme  email=a@…  attrs=2 alan  (204)
/// PUT {"attributes":{"branch_codes":["EET"]}} → firstName=NULL    email=NULL attrs=1 alan  (204)
/// </code>
///
/// <para><b>İkisi de 204 döner.</b> Kayıp ne çağıranda ne logda görünür; haftalar sonra
/// "personel listesinde ad sütunu boş" diye ortaya çıkar (#190). Bu yüzden karar bir yorumda
/// değil, <b>fırlatan</b> bir kuralda durur.</para>
///
/// <para>Öznitelik yazmak isteyen taraf gövdeyi <b>taze bir GET'ten</b> kurmalıdır; o zaman
/// silinecek alan kalmaz.</para>
/// </summary>
public static class KeycloakUserWritePolicy
{
    /// <summary>Keycloak kullanıcı temsilinde öznitelik haritasının anahtarı.</summary>
    public const string AttributesKey = "attributes";

    /// <summary>
    /// Verilen gövdenin <b>kısmi</b> güncelleme olarak gönderilmesi güvenli mi?
    /// <c>attributes</c> içeren gövde kısmi değildir.
    /// </summary>
    public static bool IsSafePartialBody(IEnumerable<string> fieldNames) =>
        !fieldNames.Contains(AttributesKey, StringComparer.Ordinal);

    /// <summary>
    /// Kısmi yazma yolunda çağrılır. Gövde kısmi değilse <b>fırlatır</b> — sessizce süzmek,
    /// hiç yazılmamış bir öznitelik güncellemesini başarılı göstermek olurdu.
    /// </summary>
    /// <exception cref="ArgumentException">Gövdede <see cref="AttributesKey"/> varsa.</exception>
    public static void EnsureSafePartialBody(IEnumerable<string> fieldNames)
    {
        if (IsSafePartialBody(fieldNames))
            return;

        throw new ArgumentException(
            $"'{AttributesKey}' kısmi gövdeyle gönderilemez: Keycloak isteği tam profil yazımı "
            + "sayar ve gövdede geçmeyen firstName/email alanlarını 204 dönerek siler (#190). "
            + "Öznitelik yazan yol gövdeyi taze bir GET'ten kurmalıdır.",
            nameof(fieldNames));
    }
}
