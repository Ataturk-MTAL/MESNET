using MESNET.Common.Shared.Tenancy;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kimlik ve kiracı katmanı belgeleri <b>paylaşımlı</b> kalmalı (#149).
///
/// <para><b>Neden kilit gerekiyor:</b> bu iki belge, kiracının kendisini <i>çözmek</i> için
/// okunur — yani kiracı daha belli değilken. Kiracıya ait sınıflandırılsalardı sorgular kendi
/// kiracılarına göre süzülürdü ve <b>hiç satır dönmezdi</b>: <c>UserAccount</c> bulunamayınca
/// yetkilendirme token'a düşer, <c>Institution</c> bulunamayınca arka plan işleri hiçbir kiracı
/// göremez. İkisi de <b>sessiz</b> bozulmalardır — istisna değil, boş sonuç.</para>
///
/// <para><c>Institution</c> ayrıca kavramsal olarak da kiracıya ait olamaz: <b>kiracının
/// kendisidir</b>. Kendi kimliğiyle damgalansaydı bile onu okumak için kimliği önceden bilmek
/// gerekirdi — döngüsel.</para>
/// </summary>
public sealed class IdentityLayerTenancyTests
{
    [Theory]
    [InlineData("UserAccount", "yetkilendirme bu kayıttan kurulur; kiracı henüz çözülmemiştir")]
    [InlineData("Institution", "kiracının kendisidir, kiracıya ait olamaz")]
    public void Kimlik_katmani_belgeleri_kiraciya_ait_degildir(string documentType, string reason)
    {
        DocumentTenancyMap.All.ShouldContainKey(documentType);

        DocumentTenancyMap.All[documentType].ShouldBe(
            DocumentTenancy.Identity,
            $"{documentType} kimlik katmanında kalmalı — {reason}. Kiracıya ait yapılırsa sorgu "
            + "boş döner ve hata vermez; bozulma sessizdir.");
    }

    /// <summary>
    /// Damgayı uygulayan politika yalnız <see cref="DocumentTenancy.Tenant"/> girişlerine bakar.
    /// Kimlik katmanının damgalanmaması bu yüzden <b>sınıflandırmanın</b> sonucudur; ayrı bir
    /// istisna listesi yoktur ve olmamalıdır — iki liste zamanla birbirinden kayar.
    /// </summary>
    [Fact]
    public void Damga_yalniz_kiraciya_ait_belgelere_uygulanir()
    {
        DocumentTenancyMap.All.Values
            .Count(t => t == DocumentTenancy.Tenant)
            .ShouldBeGreaterThan(0);

        DocumentTenancyMap.All.Values
            .ShouldNotContain(DocumentTenancy.MissingKey,
                "Kiracı verisi taşıyıp kiracı anahtarı olmayan belge, çok okullu yapıda sızıntı "
                + "yüzeyidir: sorgu iki okulun satırını ayırt edemez (#147).");
    }

    /// <summary>
    /// Kapsam dışı işlerin kiracısı bir okul kimliğiyle karışmamalı; adı olmalı.
    /// <c>*DEFAULT*</c> gibi "kimin olduğu belirsiz" bir kova bu iş için yeterli değildir —
    /// zaten yasaklandığı için bu sabit var.
    /// </summary>
    [Fact]
    public void Platform_kiracisi_okul_kimligiyle_karismaz()
    {
        Guid.TryParse(TenantResolution.Platform, out _).ShouldBeFalse();
        TenantResolution.Platform.ShouldNotBe("*DEFAULT*");
    }
}
