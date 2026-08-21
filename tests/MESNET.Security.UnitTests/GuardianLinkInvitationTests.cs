using System.Reflection;
using System.Text.RegularExpressions;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Core.Entities;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Veli–öğrenci bağı <b>davet anında</b> kurulur (#271).
///
/// <para><b>Bulunan boşluk:</b> <c>UserAccount.LinkedStudentIds</c>'i dolduran otomatik hiçbir
/// yol yoktu — ne consumer, ne seeder, ne backfill ucu. Tek yazma yolu elle
/// <c>POST /api/security/users/{id}/students</c>'ti. Karşılaştırın: <c>StudentId</c> için
/// <c>StudentAccountSyncConsumer</c> (#230), <c>BranchCodes</c> için
/// <c>resync-branch-codes</c> var.</para>
///
/// <para><b>Neden otomatik eşleştirme değil:</b> ortak anahtar yok. <c>UserAccount</c>'ta TC
/// alanı bulunmuyor ve <c>StudentRegistered</c> veli bilgisi taşımıyor; ad eşleştirmesi
/// güvenilmez. Kapsam anahtarını davete koymak <c>InstitutionId</c>/<c>BusinessId</c> ile
/// birebir aynı yerleşik desendir.</para>
///
/// <para><b>Neden önemli:</b> bağ kurulmadan md. 36 (4) tebligatı (#247) veliye
/// <b>hiç ulaşmaz</b> ve bu sessizdir.</para>
/// </summary>
public sealed class GuardianLinkInvitationTests
{
    private static readonly string HandlerKaynak = StripComments(File.ReadAllText(HandlerPath()));

    // ─── Sözleşme ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Davet_ogrenci_bagi_tasiyabilir()
    {
        typeof(CreateInvitation).GetProperty("StudentIds").ShouldNotBeNull();
    }

    /// <summary>
    /// Alan <b>sona ve varsayılanlı</b> eklendi: mevcut çağrılar bozulmadan derlenir.
    /// </summary>
    [Fact]
    public void Yeni_alan_varsayilanli_ve_sonda()
    {
        var parametreler = typeof(CreateInvitation).GetConstructors().Single().GetParameters();

        parametreler[^1].Name.ShouldBe("StudentIds");
        parametreler[^1].HasDefaultValue.ShouldBeTrue();
    }

    [Fact]
    public void Davet_kaydi_bagi_saklar()
    {
        typeof(UserInvitation).GetProperty("StudentIds").ShouldNotBeNull(
            "Bağ, davet kabul edilene kadar saklanmalı.");
    }

    // ─── Kabul anında bağ kurulur ────────────────────────────────────────────────────

    /// <summary>
    /// <b>Asıl kilit.</b> Davet tamamlanınca bağ <c>UserAccount</c>'a yazılmalı; yazılmazsa
    /// davete girilen öğrenci bilgisi sessizce kaybolur.
    /// </summary>
    [Fact]
    public void Kabul_aninda_hesaba_yazilir()
    {
        HandlerKaynak.Contains("LinkedStudentIds = invitation.StudentIds", StringComparison.Ordinal)
            .ShouldBeTrue("Davetteki bağ hesaba geçmezse sessizce kaybolur.");
    }

    /// <summary>
    /// Keycloak özniteliği de kurulur. Claim <b>otoriter değildir</b>
    /// (<c>PermissionClaimsTransformation</c> her istekte kayıttan yeniden yazar), ama öznitelik
    /// yoksa kullanıcı yönetimi ekranında bağ görünmez ve tutarsızlık doğar.
    /// </summary>
    [Fact]
    public void Keycloak_ozniteligi_de_kurulur()
    {
        HandlerKaynak.Contains("LinkedStudentClaims.ClaimType", StringComparison.Ordinal)
            .ShouldBeTrue();
    }

    // ─── Kapsam koruması ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Bağ yalnız VELİ rolünde kurulabilir.</b> Başka bir role öğrenci bağlamak, o kullanıcıya
    /// <c>ParentScopePolicy</c> üzerinden öğrenci verisine erişim açardı — izin erişimi açar,
    /// kapsamı BAĞ belirler (ADR-0001).
    /// </summary>
    [Fact]
    public void Bag_yalniz_veli_rolunde_kurulabilir()
    {
        var kaynak = StripComments(File.ReadAllText(ValidatorPath()));

        kaynak.Contains("MesnetRoles.Parent", StringComparison.Ordinal).ShouldBeTrue(
            "Öğrenci bağı yalnız veli rolünde anlamlıdır.");
        kaynak.Contains("StudentIds", StringComparison.Ordinal).ShouldBeTrue();
    }

    [Fact]
    public void Veli_rolu_tanimli()
    {
        MesnetRoles.Parent.ShouldBe("Parent");
    }

    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        var withoutXmlDoc = Regex.Replace(withoutBlocks, @"^\s*///.*$", string.Empty, RegexOptions.Multiline);
        return Regex.Replace(withoutXmlDoc, @"//.*$", string.Empty, RegexOptions.Multiline);
    }

    private static string HandlerPath() => Path.Combine(RepoRoot(),
        "src/Modules/Security/MESNET.Security.Application/Handlers/InvitationHandler.cs");

    private static string ValidatorPath() => Path.Combine(RepoRoot(),
        "src/Modules/Security/MESNET.Security.Application/Validators/CreateInvitationValidator.cs");

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
