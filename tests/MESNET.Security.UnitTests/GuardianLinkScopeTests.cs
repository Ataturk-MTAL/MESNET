using System.Text.RegularExpressions;
using MESNET.Security.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Veli–öğrenci bağı <b>kiracı sınırını aşamaz</b> (#271).
///
/// <para><b>Bulunan açık (IDOR):</b> bağ kurulurken öğrenci kimlikleri <b>istekten</b> geliyordu
/// ve hangi okulun öğrencisi olduğu <b>hiç kontrol edilmiyordu</b> — hem davet yolunda hem elle
/// bağlama yolunda. Bir okulun yöneticisi başka okulun öğrenci kimliğini vererek kendi
/// kullanıcısına o öğrencinin verisine erişim açabilirdi; <c>ParentScopeGuard</c> yalnız
/// <c>LinkedStudentIds</c>'e bakar, o listenin nasıl dolduğunu sorgulamaz.</para>
///
/// <para>CLAUDE.md: <b>"permission erişimi açar, KAPSAMI belirlemez — kapsam istekten
/// ALINMAZ"</b>.</para>
/// </summary>
public sealed class GuardianLinkScopeTests
{
    private static readonly Guid Bizim = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BizimIkinci = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BaskaOkulun = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private static readonly IReadOnlySet<Guid> Kiracidakiler =
        new HashSet<Guid> { Bizim, BizimIkinci };

    [Fact]
    public void Kiracinin_ogrencisi_baglanabilir()
    {
        GuardianLinkScopePolicy.AllInScope([Bizim, BizimIkinci], Kiracidakiler).ShouldBeTrue();
    }

    /// <summary><b>Asıl regresyon.</b> Başka okulun öğrencisi bağlanamaz.</summary>
    [Fact]
    public void Baska_okulun_ogrencisi_baglanamaz()
    {
        GuardianLinkScopePolicy.AllInScope([BaskaOkulun], Kiracidakiler)
            .ShouldBeFalse("Kapsam istekten alınamaz — başka okulun öğrencisine erişim açardı.");
    }

    /// <summary>Karışık listede tek bir yabancı kimlik bile tümünü reddettirir.</summary>
    [Fact]
    public void Karisik_listede_yabanci_kimlik_reddedilir()
    {
        GuardianLinkScopePolicy.FindOutOfScope([Bizim, BaskaOkulun], Kiracidakiler)
            .ShouldBe([BaskaOkulun]);
    }

    [Fact]
    public void Bos_liste_gecerlidir()
    {
        GuardianLinkScopePolicy.AllInScope([], Kiracidakiler).ShouldBeTrue(
            "Bağı kaldırmak geçerli bir işlemdir.");
    }

    /// <summary>
    /// <b>Kapalı tarafa düşer.</b> Kiracı görünümü boşsa (dağıtımda <c>resync-projections</c>
    /// hiç koşmadıysa) her atama reddedilir — kapsamsız kalmak, yanlış kapsama düşmekten iyidir
    /// (ADR-0003 adım 2 ile aynı yön). Hata mesajı operatörü resync'e yönlendirir.
    /// </summary>
    [Fact]
    public void Gorunum_bossa_hepsi_reddedilir()
    {
        GuardianLinkScopePolicy.AllInScope([Bizim], new HashSet<Guid>()).ShouldBeFalse();
    }

    [Fact]
    public void Tekrar_eden_yabanci_kimlik_bir_kez_bildirilir()
    {
        GuardianLinkScopePolicy.FindOutOfScope([BaskaOkulun, BaskaOkulun], Kiracidakiler)
            .Count.ShouldBe(1);
    }

    // ─── İki kapı da korunuyor ───────────────────────────────────────────────────────

    /// <summary>
    /// Bağ iki yoldan kurulabiliyor: davet (<c>CreateInvitation.StudentIds</c>) ve elle bağlama
    /// (<c>ChangeUserStudents</c>). <b>İkisinde de</b> kontrol olmalı — biri açık kalırsa açık
    /// tümüyle açıktır.
    /// </summary>
    [Theory]
    [InlineData("src/Modules/Security/MESNET.Security.Application/Handlers/InvitationHandler.cs", "davet")]
    [InlineData("src/Modules/Security/MESNET.Security.Application/Handlers/UserManagementHandler.cs", "elle bağlama")]
    public void Her_iki_baglama_yolu_kapsam_kontrolunden_geciyor(string yol, string ad)
    {
        var kaynak = StripComments(File.ReadAllText(Path.Combine(RepoRoot(), yol)));

        kaynak.Contains("GuardianLinkScopeGuard.EnsureInScopeAsync", StringComparison.Ordinal)
            .ShouldBeTrue($"'{ad}' yolu kapsam kontrolünden geçmiyor.");
    }

    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        var withoutXmlDoc = Regex.Replace(withoutBlocks, @"^\s*///.*$", string.Empty, RegexOptions.Multiline);
        return Regex.Replace(withoutXmlDoc, @"//.*$", string.Empty, RegexOptions.Multiline);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx).");
    }
}
