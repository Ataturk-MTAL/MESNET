using MESNET.Business.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Business.UnitTests;

/// <summary>
/// Vergi kimliği biçim kuralı (#150).
///
/// <para>Alan paylaşımlı işletme kataloğunun <b>doğal anahtarıdır</b>: iki okulun aynı firmayı
/// ayrı ayrı kaydetmesini engelleyen tek şey odur. Kopyaları sonradan birleştirmek —
/// sözleşmelerin, koordinasyon görünümlerinin ve devamsızlık kayıtlarının yeniden
/// yönlendirilmesi — çok daha pahalıdır.</para>
/// </summary>
public sealed class TaxNumberPolicyTests
{
    /// <summary>Tüzel kişi 10 hane (VKN), şahıs işletmesi 11 hane (TCKN).</summary>
    [Theory]
    [InlineData("1234567890")]
    [InlineData("12345678901")]
    public void Gecerli_uzunluklar_kabul_edilir(string value)
    {
        TaxNumberPolicy.IsValid(value).ShouldBeTrue();
    }

    /// <summary>
    /// Şahıs işletmeleri TCKN kullanır ve mesleki eğitimde staj veren işletmelerin önemli bir
    /// bölümü şahıs işletmesidir; yalnız 10 haneyi kabul etmek onları sisteme alamamak olurdu.
    /// </summary>
    [Fact]
    public void Sahis_isletmesi_tckn_ile_kaydedilebilir()
    {
        TaxNumberPolicy.IsValid("12345678901").ShouldBeTrue();
    }

    [Theory]
    [InlineData("123456789", "dokuz hane")]
    [InlineData("123456789012", "on iki hane")]
    [InlineData("123456789O", "harf içeriyor (O)")]
    [InlineData("1234 567890", "boşluk içeriyor")]
    [InlineData("123456789.", "noktalama içeriyor")]
    public void Gecersiz_bicimler_reddedilir(string value, string reason)
    {
        TaxNumberPolicy.IsValid(value).ShouldBeFalse(reason);
    }

    /// <summary>
    /// Boş değer <b>geçersizdir</b>. Boş bırakılabilseydi iki okul aynı firmayı boş anahtarla
    /// kaydeder ve kopya tam da engellenmek istenen yerde doğardı.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_deger_gecersizdir(string? value)
    {
        TaxNumberPolicy.IsValid(value).ShouldBeFalse();
        TaxNumberPolicy.Normalize(value).ShouldBeNull();
    }

    /// <summary>
    /// Benzersizlik <b>normalleştirilmiş</b> değer üzerinden kurulur. Kenar boşlukları
    /// atılmasaydı <c>" 1234567890"</c> ile <c>"1234567890"</c> iki ayrı kayıt olur ve kısıt
    /// hiçbir şey korumazdı.
    /// </summary>
    [Fact]
    public void Kenar_bosluklari_atilir_yoksa_kisit_bosa_cikar()
    {
        TaxNumberPolicy.Normalize("  1234567890  ").ShouldBe("1234567890");
        TaxNumberPolicy.Normalize(" 1234567890").ShouldBe(TaxNumberPolicy.Normalize("1234567890"));
    }
}
