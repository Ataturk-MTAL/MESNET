namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Bir kurum alt ağacındaki OKUL kiracılarının listesi.
///
/// <para><b>Neden arayüz, doğrudan sorgu değil:</b> kiracı = okul (ADR-0003) ve okul kaydı
/// Institution modülünündür. Başka bir modülün <c>institution</c> şemasına sorgu atması şema
/// izolasyonunu kırardı. Uygulaması modülde, sözleşmesi burada —
/// <see cref="ITenantDirectory"/> ile aynı desen.</para>
///
/// <para><b>İl ve ilçe düğümleri kiracı DEĞİLDİR</b> ve kiracı damgalı hiçbir veri taşımazlar;
/// bu yüzden döndürülen liste yalnız okul düğümlerini içerir. Süzülmeselerdi çağıran hiçbir
/// verinin bulunmadığı "kiracılarda" arama yapardı — istisna değil, sessiz boş sonuç.</para>
///
/// <para><b>Boş liste hata değildir:</b> alt ağaçta okul yoksa çağıranın arayacağı bir şey de
/// yoktur.</para>
/// </summary>
public interface IInstitutionSubtreeDirectory
{
    /// <param name="pathPrefix">
    /// Aktörün kurum ağacındaki yolu (<c>InstitutionVisibility.PathPrefix</c>). Bu önekle
    /// başlayan okullar döner.
    /// </param>
    Task<IReadOnlyList<string>> GetSchoolTenantsAsync(
        string pathPrefix, CancellationToken cancellationToken = default);
}
