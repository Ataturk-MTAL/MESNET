using Microsoft.Extensions.Logging;

namespace MESNET.Coordination.Application.Helpers;

/// <summary>
/// İşletme olaylarındaki <b>provenance</b> değerini koordinasyon <b>kapsamına</b> çevirir
/// (ADR-0003 adım 4).
///
/// <para><b>Bugünkü yaklaşım tek kurumludur.</b> Kaydı giren okul, koordinasyon kapsamı
/// sayılıyor. Faz 1'de tek kurum olduğu için ikisi her zaman aynı. Çok okullu yapıda YANLIŞ
/// olur: aynı işletmeye ikinci okuldan öğrenci yerleştirildiğinde o okul işletmeyi
/// koordinasyon ekranlarında göremez, çünkü görünüm ilk kaydedenin kimliğiyle açılmıştır.
/// Doğrusu kapsamı ilişkiden (yerleştirme) türetmektir — ayrı domain migration.</para>
///
/// <para><b>Neden boş değer uyarı üretir:</b> kayıt uçları kurum kapsamı olmadan işletme
/// oluşturmayı zaten reddeder, yani boş provenance ancak <c>InstitutionId</c> →
/// <c>RegisteredByInstitutionId</c> JSON göçü <b>atlanmış</b> belgelerden gelir. Uyarı
/// olmasa görünüm <c>Guid.Empty</c> kapsamıyla yazılır ve işletme koordinasyon ekranlarından
/// <b>sessizce kaybolur</b> — hata yok, log yok, boş liste.</para>
/// </summary>
public static class BusinessScopeOrigin
{
    public static Guid Resolve(Guid registeredByInstitutionId, Guid businessId, ILogger logger)
    {
        if (registeredByInstitutionId == Guid.Empty)
        {
            logger.LogWarning(
                "İşletmenin kayıt eden kurum bilgisi boş: {BusinessId}. Koordinasyon görünümü " +
                "kapsamsız açılacak ve işletme ekranlarda görünmeyecek. Olası neden: " +
                "business.mt_doc_business belgelerinde 'institutionId' → " +
                "'registeredByInstitutionId' JSON göçü çalıştırılmamış (ADR-0003 adım 4).",
                businessId);
        }

        return registeredByInstitutionId;
    }
}
