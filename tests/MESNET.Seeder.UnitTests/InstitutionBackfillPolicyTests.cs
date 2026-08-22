using System.Text.Json;
using MESNET.Seeder.Seeders;
using Shouldly;
using Xunit;

namespace MESNET.Seeder.UnitTests;

/// <summary>
/// Kurum il/ilçe tamamlama kararı (#196).
///
/// <para><b>Yaşanan hata:</b> koruma yalnız <c>provinceCode</c>'a bakıp doluysa erken dönüyordu,
/// oysa PATCH gövdesi il ve ilçeyi <b>birlikte</b> yazıyordu. Il bir kez dolduktan sonra ilçe
/// kalıcı olarak boş kalıyor ve hiçbir koşu onu tamamlamıyordu. Canlı veride tam olarak bu
/// olmuştu: il <c>33</c>, ilçe <c>null</c>.</para>
///
/// <para><b>Neden sessiz:</b> ilçe yalnız <i>doluysa</i> doğrulanır — boş bırakmak geçerlidir.
/// Ne hata çıkar ne uyarı; eksik alan yalnız ilçe bazlı gruplama/filtreleme yapılınca ortaya
/// çıkar, o da kurumu sessizce kapsam dışında bırakır.</para>
/// </summary>
public sealed class InstitutionBackfillPolicyTests
{
    private static JsonElement Kurum(string json) => JsonDocument.Parse(json).RootElement;

    /// <summary><b>Asıl regresyon:</b> il dolu, ilçe boş → iş BİTMEMİŞTİR.</summary>
    [Fact]
    public void Il_dolu_ilce_bossa_ilce_tamamlanmali()
    {
        var (province, district) = InstitutionBackfillPolicy.MissingFields(
            Kurum("""{"provinceCode":"33","districtName":null}"""));

        province.ShouldBeFalse("Dolu alan üzerine yazılmamalı.");
        district.ShouldBeTrue("İl dolu diye erken dönülürse ilçe kalıcı boş kalır.");
    }

    [Fact]
    public void Ilce_alani_hic_yoksa_da_tamamlanmali()
    {
        var (_, district) = InstitutionBackfillPolicy.MissingFields(Kurum("""{"provinceCode":"33"}"""));

        district.ShouldBeTrue();
    }

    [Fact]
    public void Ikisi_de_bossa_ikisi_de_tamamlanmali()
    {
        InstitutionBackfillPolicy.MissingFields(Kurum("{}"))
            .ShouldBe((true, true));
    }

    [Fact]
    public void Ikisi_de_doluysa_yapilacak_is_yok()
    {
        InstitutionBackfillPolicy.MissingFields(
            Kurum("""{"provinceCode":"34","districtName":"Kadıköy"}"""))
            .ShouldBe((false, false));
    }

    /// <summary>Yalnız boşluktan oluşan değer "dolu" sayılmaz — kapsam kararında işe yaramaz.</summary>
    [Fact]
    public void Bosluktan_ibaret_deger_dolu_sayilmaz()
    {
        InstitutionBackfillPolicy.MissingFields(
            Kurum("""{"provinceCode":"  ","districtName":""}"""))
            .ShouldBe((true, true));
    }

    /// <summary>
    /// Elle seçilmiş il/ilçe geri alınmamalı: kural yalnız <b>boş</b> alanı doldurmayı söyler,
    /// varsayılana çekmeyi değil.
    /// </summary>
    [Fact]
    public void Elle_secilmis_farkli_il_uzerine_yazilmaz()
    {
        var (province, _) = InstitutionBackfillPolicy.MissingFields(
            Kurum("""{"provinceCode":"06","districtName":"Çankaya"}"""));

        province.ShouldBeFalse();
    }
}
