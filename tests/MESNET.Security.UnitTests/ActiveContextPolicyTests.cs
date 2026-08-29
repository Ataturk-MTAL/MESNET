using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Aktif bağlamın kullanılabilirlik kararı.
///
/// <para><b>Neden saf bir fonksiyon:</b> aynı karar iki yerde veriliyor — kiracı
/// çözümlemesinde ve izin dönüşümünde. İki kopya olsaydı biri değişip diğeri kalırdı ve
/// bayat bir bağlam yalnız birinde düşerdi: kullanıcı bir ekranda A okulunu, diğerinde B
/// okulunu görürdü.</para>
///
/// <para><b>Geçersiz bağlam HATA DEĞİLDİR.</b> <c>null</c> döner ve çağıran ev kurumuna
/// düşer. Bayat bağlam bir yetki ihlali değil, bir zamanaşımıdır; kullanıcı okulu yeniden
/// seçer.</para>
/// </summary>
public sealed class ActiveContextPolicyTests
{
    private static readonly Guid Okul = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string IlYolu = "/il/";
    private const string OkulYolu = "/il/ilce/okul/";
    private const string BaskaIlinOkuluYolu = "/baska-il/ilce/okul/";

    [Fact]
    public void Gecerli_baglam_kurum_kimligini_dondurur()
    {
        var sonuc = ActiveContextPolicy.Resolve(
            activeInstitutionId: Okul,
            storedSessionId: "oturum-1",
            currentSessionId: "oturum-1",
            actorPath: IlYolu,
            targetPath: OkulYolu);

        sonuc.ShouldBe(Okul);
    }

    [Fact]
    public void Baglam_yoksa_null_doner()
    {
        ActiveContextPolicy.Resolve(null, "oturum-1", "oturum-1", IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Bos_Guid_baglam_sayilmaz()
    {
        ActiveContextPolicy.Resolve(Guid.Empty, "oturum-1", "oturum-1", IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Bayat_oturumda_baglam_dusurulur()
    {
        // Yeni girişte sid değişir (ölçüldü). Dünkü seçim bugün geçerli değildir.
        ActiveContextPolicy.Resolve(Okul, "oturum-1", "oturum-2", IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Saklanmis_oturum_kimligi_yoksa_baglam_dusurulur()
    {
        // Kimliksiz saklanmış bağlam hiçbir oturuma ait değildir; süresiz yaşamamalı.
        ActiveContextPolicy.Resolve(Okul, null, "oturum-1", IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Istekte_oturum_kimligi_yoksa_baglam_dusurulur()
    {
        // Token'da sid gelmiyorsa bağlamın hangi oturumda kurulduğu doğrulanamaz.
        ActiveContextPolicy.Resolve(Okul, "oturum-1", null, IlYolu, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Alt_agac_disindaki_hedef_dusurulur()
    {
        // AĞAÇ DEĞİŞEBİLİR: okul başka ilçeye taşınabilir. Yalnız yazma anında doğrulanan
        // bir bağlam sessizce yetki taşımaya devam ederdi.
        ActiveContextPolicy.Resolve(Okul, "oturum-1", "oturum-1", IlYolu, BaskaIlinOkuluYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Aktorun_yolu_yoksa_baglam_dusurulur()
    {
        // Geçiş ucu koşmamış aktör alt ağaç iddiasında bulunamaz.
        ActiveContextPolicy.Resolve(Okul, "oturum-1", "oturum-1", null, OkulYolu)
            .ShouldBeNull();
    }

    [Fact]
    public void Hedefin_yolu_yoksa_baglam_dusurulur()
    {
        ActiveContextPolicy.Resolve(Okul, "oturum-1", "oturum-1", IlYolu, null)
            .ShouldBeNull();
    }

    [Fact]
    public void Oturum_kimligi_karsilastirmasi_buyuk_kucuk_harfe_duyarlidir()
    {
        // sid rastgele üretilmiş bir dizedir; harf katlaması iki ayrı oturumu eşitleyebilir.
        ActiveContextPolicy.Resolve(Okul, "AbC", "abc", IlYolu, OkulYolu)
            .ShouldBeNull();
    }
}
