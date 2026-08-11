using System.Text.RegularExpressions;
using MESNET.Common.Shared.Tenancy;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Okul kimliği → kiracı kimliği eşleşmesi <b>tek noktada</b> yaşar (#148).
///
/// <para><b>#148'in önerdiği mekanizma gerekmedi.</b> Issue <c>ITenantContext</c> + merkezî bir
/// Marten session factory istiyordu; gerekçesi "her yeni handler kiracı-kör yazılıyor, retrofit
/// maliyeti doğrusal birikiyor" idi. Retrofit yapıldığında (ADR-0003 adım 5) ölçüldü:
/// <b>219 çağrı yerinin hiçbirine dokunulmadı</b> — kiracıyı tek bir middleware
/// <c>IMessageBus.TenantId</c> üzerine koydu, Wolverine handler'lara ve cascading mesajlara
/// devretti, Marten conjoined satırları süzdü. Session factory'ye hiç ihtiyaç olmadı.</para>
///
/// <para><b>Ama issue'nun KURALI yaşıyor:</b> "hiçbir kod <c>tenantId == institutionId</c>
/// anlamsal varsayımı yapmayacak". Çevrim iki ayrı yerde duruyordu — istek yolunda
/// (<c>TenantResolution.Resolve</c>) ve arka plan kiracı dizininde. Biri değişip diğeri
/// kalsaydı zamanlanmış işler <b>hiçbir verinin kullanmadığı</b> bir kiracıda koşardı ve sonuç
/// istisna değil <b>boş sonuç</b> olurdu: rapor üretilmez, maaş dönemi açılmaz, log temiz kalır.</para>
/// </summary>
public sealed class TenantIdentityMappingTests
{
    private static readonly Guid Okul = Guid.Parse("efd57b88-2f47-471c-9f51-476f80fabfca");

    [Fact]
    public void Istek_yolu_ve_cevrim_ayni_kiraciyi_uretir()
    {
        TenantResolution.Resolve(Okul, ["institution:view"])
            .ShouldBe(TenantResolution.ForInstitution(Okul));
    }

    /// <summary>
    /// Kiracı kimliği, damgalama betiğinin ve mevcut satırların kullandığı biçimle aynı olmalı.
    /// Biçim değişirse (büyük harf, süslü parantez) hiçbir sorgu eşleşmez ve <b>hata da vermez</b>.
    /// </summary>
    [Fact]
    public void Kiraci_kimligi_kucuk_harfli_tireli_guid_bicimidir()
    {
        TenantResolution.ForInstitution(Okul).ShouldBe("efd57b88-2f47-471c-9f51-476f80fabfca");
    }

    /// <summary>
    /// Arka plan kiracı dizini çevrimi <b>kopyalamamalı</b>, tek noktayı çağırmalı. Kopya,
    /// eksenin taşındığı gün sessizce geride kalır.
    /// </summary>
    [Fact]
    public void Kiraci_dizini_cevrimi_kopyalamaz()
    {
        var source = File.ReadAllText(Path.Combine(
            RepoRoot(), "src",
            "Modules/Institution/MESNET.Institution.Application/Services/InstitutionTenantDirectory.cs"));

        source.Contains("TenantResolution.ForInstitution", StringComparison.Ordinal).ShouldBeTrue(
            "Kiracı dizini çevrimi kendi yapıyor. 1:1 eşleşme tek noktada kalmalı (#148); "
            + "aksi hâlde istek yolu değişip burası kalınca arka plan işleri hiçbir verinin "
            + "kullanmadığı bir kiracıda koşar ve sonuç sessizce boş olur.");

        Regex.IsMatch(source, @"\.Select\(\s*id\s*=>\s*id\.ToString\(\)").ShouldBeFalse(
            "Ham ToString() çevrimi geri gelmiş.");
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

        throw new InvalidOperationException(
            $"Depo kökü bulunamadı (MESNET.slnx aranıyordu): {AppContext.BaseDirectory}");
    }
}
