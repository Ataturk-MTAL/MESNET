namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Bir kurum alt ağacındaki kurum kimlikleri ve kiracılarının sorgulanması.
///
/// <para><b>Neden arayüz, doğrudan sorgu değil:</b> kiracı = okul (ADR-0003) ve okul kaydı
/// Institution modülünündür. Başka bir modülün <c>institution</c> şemasına sorgu atması şema
/// izolasyonunu kırardı. Uygulaması modülde, sözleşmesi burada —
/// <see cref="ITenantDirectory"/> ile aynı desen.</para>
///
/// <para><b>Metotlar ayrı filtreleme politikaları uygular.</b> <see cref="GetSchoolTenantsAsync"/>
/// yalnız okul düğümlerini döndürür (kiracılar), <see cref="GetSubtreeInstitutionIdsAsync"/> ise
/// tüm düğüm türlerini (okul, ilçe, il) ve müdürlük personeli kaydı gibi ilgili tüm kurum
/// kimlikleri döndürür.</para>
///
/// <para><b>Boş liste hata değildir:</b> alt ağaçta eşleşen kurum yoksa çağıranın arayacağı bir
/// şey de yoktur.</para>
/// </summary>
public interface IInstitutionSubtreeDirectory
{
    /// <param name="pathPrefix">
    /// Aktörün kurum ağacındaki yolu (<c>InstitutionVisibility.PathPrefix</c>). Bu önekle
    /// başlayan okullar döner.
    /// </param>
    Task<IReadOnlyList<string>> GetSchoolTenantsAsync(
        string pathPrefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Yol öneki altındaki <b>bütün</b> kurum kimlikleri — okul, ilçe ve il düğümleri dahil.
    ///
    /// <para><b>Neden <see cref="GetSchoolTenantsAsync"/> yetmez:</b> o metot bilerek yalnız
    /// okul düğümünü döndürür, çünkü kiracı = okul. Ama kullanıcı ve davet kayıtları müdürlük
    /// düğümüne de bağlanabilir: müdürlük personelinin <c>InstitutionId</c>'si il/ilçe
    /// düğümüdür. Okul listesiyle daraltılsaydı müdürlük <b>kendi ekibini</b> göremezdi —
    /// hata değil, sessiz boş liste.</para>
    ///
    /// <para><b>Kiracı kimliği DEĞİL kurum kimliği döner.</b> Çağıran bunları kiracı olarak
    /// kullanmamalıdır; müdürlük düğümleri kiracı değildir.</para>
    /// </summary>
    Task<IReadOnlyList<Guid>> GetSubtreeInstitutionIdsAsync(
        string pathPrefix, CancellationToken cancellationToken = default);
}
