using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kurum (kiracı) bağını kimin yazabileceği (ADR-0003 adım 2).
///
/// <para>Faz 1'de tek kurum var, yani bu kural pratikte hep sağlanıyor. Test bugün için değil
/// <b>çok kiracılığa geçilen gün</b> için var: kiracı anahtarını yazan uç bu kontrol olmadan
/// açılırsa, o gün her okul yöneticisi başka okulun kullanıcısını kendi kiracısına çekebilir
/// ve bunu fark ettiren hiçbir sinyal olmaz.</para>
/// </summary>
public sealed class UserInstitutionScopePolicyTests
{
    private static readonly Guid Own = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Other = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ─── Aktörün kendi kapsamı ──────────────────────────────────────────────────────────

    [Fact]
    public void Kapsamsiz_aktor_kiraci_anahtari_yazamaz()
    {
        UserInstitutionScopePolicy.CanAssign(null, null, Own).ShouldBeFalse();
    }

    /// <summary>Boş Guid de kapsamsızlıktır — "kurum yok" ile aynı anlama gelir.</summary>
    [Fact]
    public void Bos_guid_kapsam_sayilmaz()
    {
        UserInstitutionScopePolicy.CanAssign(Guid.Empty, null, Own).ShouldBeFalse();
    }

    // ─── Hedef kurum ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Bagsiz_kullanici_aktorun_kendi_kurumuna_baglanabilir()
    {
        UserInstitutionScopePolicy.CanAssign(Own, null, Own).ShouldBeTrue();
    }

    [Fact]
    public void Baska_kuruma_yazmak_hicbir_aktorun_yetkisinde_degil()
    {
        UserInstitutionScopePolicy.CanAssign(Own, null, Other).ShouldBeFalse();
    }

    /// <summary>
    /// <b>Devralma tek taraflı değildir.</b> Başka kuruma bağlı kullanıcı, önce o kurum bağı
    /// çözmeden alınamaz — aksi hâlde kiracı sınırı tek bir POST ile aşılırdı.
    /// </summary>
    [Fact]
    public void Baska_kuruma_bagli_kullanici_devralinamaz()
    {
        UserInstitutionScopePolicy.CanAssign(Own, Other, Own).ShouldBeFalse();
    }

    // ─── Bağı çözme ─────────────────────────────────────────────────────────────────────

    /// <summary>Kurumdan ayrılan personelin bağı çözülebilir; kullanıcı kapsamsız kalır.</summary>
    [Fact]
    public void Kendi_kullanicisinin_bagi_cozulebilir()
    {
        UserInstitutionScopePolicy.CanAssign(Own, Own, null).ShouldBeTrue();
    }

    [Fact]
    public void Baska_kurumun_kullanicisinin_bagi_cozulemez()
    {
        UserInstitutionScopePolicy.CanAssign(Own, Other, null).ShouldBeFalse();
    }

    /// <summary>Zaten bağsız kullanıcıyı bağsız bırakmak bir ihlal değildir.</summary>
    [Fact]
    public void Bagsizi_bagsiz_birakmak_serbesttir()
    {
        UserInstitutionScopePolicy.CanAssign(Own, null, null).ShouldBeTrue();
    }
}
