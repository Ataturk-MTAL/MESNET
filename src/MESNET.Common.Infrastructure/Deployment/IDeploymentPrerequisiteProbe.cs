namespace MESNET.Common.Infrastructure.Deployment;

/// <summary>
/// Bir dağıtım ön koşulunun <b>atlanıp atlanmadığını belirtiden ölçen</b> sonda.
///
/// <para><b>Neden belirti, neden "uç çağrıldı mı" değil:</b> resync uçlarının çağrıldığına dair
/// hiçbir kalıcı iz yoktur — damga tablosu yok, denetim kaydı ucun kendisine bakmıyor. Kalıcı
/// olan tek şey <b>sonucudur</b>: görünüm doldu mu, yol yazıldı mı, bağ kuruldu mu. Sonda o
/// sonucu okur. Yan etki: uç elle, betikle ya da başka bir sürümde çağrılmış olsa bile sonuç
/// aynı biçimde görülür.</para>
///
/// <para><b>Neden modül başına arayüz, neden tek merkezî sorgu değil:</b> ölçülen belgeler
/// modüllerin kendi şemalarındadır ve başka bir modülün oraya sorgu atması şema izolasyonunu
/// kırar. Sözleşme altyapıda, uygulama modülde — <c>ITenantDirectory</c> ile aynı desen.</para>
///
/// <para><b>Sonda yalnız OKUR.</b> Yazan bir sonda açılışta veriyi değiştirirdi; bu depoda
/// açılıştan olay yayınlamak zaten mümkün değil (Wolverine host'tan sonra başlar) ve iki resync
/// ucu idempotent değil. Ölçen açılış, koşturan operatör.</para>
/// </summary>
public interface IDeploymentPrerequisiteProbe
{
    /// <summary>İnsan okur ad — log satırında ve hata metninde geçer.</summary>
    string Name { get; }

    /// <summary>
    /// Bulgu çıkarsa koşturulacak adım — <b>birebir</b> çağrılabilir biçimde
    /// (ör. <c>POST /api/institutions/rebuild-hierarchy</c>). Kısaltma yapılmaz: operatör bunu
    /// kopyalayıp koşturur.
    /// </summary>
    string Remedy { get; }

    /// <summary>
    /// Ölçer. Ön koşul karşılanmışsa <c>null</c> döner.
    ///
    /// <para><b>İstisna fırlatmak bulgu DEĞİLDİR.</b> Ölçüm yapılamadıysa (tablo henüz yok,
    /// veritabanı erişilemez) çağıran bunu "atlandı" olarak raporlar ve açılışı durdurmaz —
    /// <c>RealmVerificationHostedService</c>'in Keycloak'a ulaşamama davranışıyla aynı çizgi.
    /// Aksi hâlde bu kontrol, ilk açılışı uygulama arızasına çevirirdi.</para>
    /// </summary>
    Task<PrerequisiteFinding?> ProbeAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Ölçülmüş bir eksiklik.
///
/// <para>İki alan ayrı tutulur çünkü ikisi ayrı işe yarar: <see cref="Symptom"/> operatörün
/// "bende de var mı" diye bakacağı <b>sayıdır</b>, <see cref="Consequence"/> ise "koşturmazsam
/// ne olur" sorusunun cevabı. Yalnız belirti yazılsaydı eksiklik önemsiz görünürdü; yalnız sonuç
/// yazılsaydı doğrulanamaz bir iddia olurdu.</para>
/// </summary>
/// <param name="Symptom">Ölçülen sayı ve neyi saydığı — yuvarlanmadan.</param>
/// <param name="Consequence">Adım koşturulmazsa kullanıcının göreceği yanlış davranış.</param>
public sealed record PrerequisiteFinding(string Symptom, string Consequence);
