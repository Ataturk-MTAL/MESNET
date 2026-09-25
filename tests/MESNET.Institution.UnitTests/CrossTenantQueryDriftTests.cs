using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Kiracılar arası okuma tek kapıdan geçer (D2).
///
/// <para><b>Neden derleyici yakalayamaz:</b> <c>AnyTenant()</c> ve <c>TenantIsOneOf(...)</c>
/// geçerli Marten çağrılarıdır ve doğru derlenirler. Yeni bir handler <c>AnyTenant()</c>
/// yazarsa hiçbir davranış testi kırılmaz — kiracılar arası okuma <b>sessizce</b> açılır ve
/// kimse fark etmez. Tek savunma, çağrının kaynakta hiç bulunmamasıdır.</para>
///
/// <para><b>Doğrusu (#317 sonrası):</b> kapsam <c>SubtreeTenantScope.ResolveAsync</c> ya da
/// <c>ITenantDirectory</c>'den türetilir; sorgu <c>CrossTenantQuery.CollectAsync</c> ile OKUL
/// BAŞINA session açar. <c>TenantIsOneOf</c> artık HİÇBİR yerde kullanılmaz: row-level security
/// yalnız session'ın kiracısını gösterir ve operatörün listesini bilmez — tek sorgu hata değil
/// EKSİK sonuç döndürürdü.</para>
/// </summary>
public sealed class CrossTenantQueryDriftTests
{
    /// <summary>Kapsamı tümden kaldıran operatör — hiçbir gerekçeyle kullanılmaz.</summary>
    private static readonly Regex AnyTenantCall = new(@"\bAnyTenant\s*\(", RegexOptions.Compiled);

    /// <summary>Kapsamı listeye daraltan operatör — RLS altında sessizce eksik sonuç verir.</summary>
    private static readonly Regex TenantIsOneOfCall =
        new(@"\bTenantIsOneOf\s*\(", RegexOptions.Compiled);

    /// <summary>Okul başına session açan kapı (#317).</summary>
    private static readonly Regex CrossTenantQueryCall =
        new(@"\bCrossTenantQuery\.CollectAsync\s*\(", RegexOptions.Compiled);

    /// <summary>
    /// Kiracılar arası okuyabilecek tek üretim dosyaları — depo köküne göre TAM YOL. Yalnız dosya
    /// adını karşılaştırmak, başka bir yerde aynı adı taşıyan bir dosyanın sessizce izinli
    /// sayılmasına yol açardı. Her birinin kiracı listesi istekten DEĞİL, sunucu tarafından gelir.
    /// </summary>
    private static readonly string[] AllowedFiles =
    [
        // İl/ilçe kapsamı — liste SubtreeTenantScope'tan (aktörün kurum yolundan).
        "src/Modules/Internship/MESNET.Internship.Application/Handlers/GetStuckApprovalsHandler.cs",
        // Dağıtım ön koşulu sondası (açılışta ÖLÇER, yazmaz) — liste ITenantDirectory'den.
        "src/Modules/Internship/MESNET.Internship.Application/Deployment/InternshipSagaDuplicateProbe.cs",
    ];

    /// <summary>
    /// Bu kilidin KENDİ dosyası — tarama artık <c>tests/</c> ağacını da kapsıyor ve bu
    /// dosya kendi ihlal mesajı dizgelerinde ("AnyTenant() ..." / "TenantIsOneOf(...) ...")
    /// yasak çağrının adını olduğu gibi taşıyor. Bunlar yorum DEĞİL, dize sabiti — StripComments
    /// onları silmez. Kendi kendine tetiklenmeyi önlemek için bu tek dosya taramadan hariç
    /// tutulur; gerçek üretim/test koduna ait değildir, gerekçenin bir parçasıdır.
    /// </summary>
    private const string SelfPath = "tests/MESNET.Institution.UnitTests/CrossTenantQueryDriftTests.cs";

    [Fact]
    public void Kaynakta_AnyTenant_cagrisi_yok()
    {
        var violations = new List<string>();

        foreach (var file in SourceFiles())
        {
            var code = StripComments(File.ReadAllText(file));
            if (AnyTenantCall.IsMatch(code))
                violations.Add(Relative(file));
        }

        violations.ShouldBeEmpty(
            "AnyTenant() kiracı kapsamını TÜMDEN kaldırır ve bu depoda yasaktır — kapsamsız "
            + "aktör için bile. Kapsamı SubtreeTenantScope.ResolveAsync ile türetip "
            + $"TenantIsOneOf(...) kullanın. İhlaller: {string.Join(" | ", violations)}");
    }

    [Fact]
    public void Kaynakta_TenantIsOneOf_cagrisi_yok()
    {
        var violations = SourceFiles()
            .Where(file => TenantIsOneOfCall.IsMatch(StripComments(File.ReadAllText(file))))
            .Select(Relative)
            .ToList();

        violations.ShouldBeEmpty(
            "TenantIsOneOf(...) row-level security altında EKSİK sonuç verir: politika yalnız "
            + "session'ın kiracısını gösterir, operatörün listesini bilmez (#317). Kiracılar arası "
            + "okumayı CrossTenantQuery.CollectAsync ile okul başına session açarak yapın. "
            + $"İhlaller: {string.Join(" | ", violations)}");
    }

    [Fact]
    public void Kiracilar_arasi_okuma_yalniz_izinli_dosyalarda()
    {
        var violations = SourceFiles()
            .Where(file => CrossTenantQueryCall.IsMatch(StripComments(File.ReadAllText(file))))
            .Select(Relative)
            .Where(path => !AllowedFiles.Contains(path, StringComparer.Ordinal))
            .ToList();

        violations.ShouldBeEmpty(
            "Kiracılar arası okuma yalnız tek kapıdan yapılır. Kiracı listesini SubtreeTenantScope "
            + "ya da ITenantDirectory'den alın, istekten ALMAYIN; dosyayı gerekçesiyle izin "
            + $"listesine ekleyin. İhlaller: {string.Join(" | ", violations)}");
    }

    /// <summary>
    /// Satır ve blok yorumlarını atar: bu kuralın NEDENİNİ anlatan XML doc'lar yasak çağrının
    /// adını geçirir. Yorumu koda saymak doğru yazılmış dosyayı ihlal gösterirdi.
    /// </summary>
    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//.*$", string.Empty, RegexOptions.Multiline);
    }

    /// <summary>
    /// Tarama <c>src/</c> ile sınırlı DEĞİLDİR: spesifikasyon "test dosyaları dahil" der
    /// — bir test bugün <c>AnyTenant()</c> çağırsa bu kilit onu göremezdi. <see cref="SelfPath"/>
    /// kilidin kendi dosyasıdır ve ayrı bir gerekçeyle hariç tutulur (yukarıya bakınız).
    /// </summary>
    private static IEnumerable<string> SourceFiles()
    {
        var repoRoot = RepoRoot();
        var roots = new[] { Path.Combine(repoRoot, "src"), Path.Combine(repoRoot, "tests") };
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return roots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal)
                     && !string.Equals(Relative(f), SelfPath, StringComparison.Ordinal));
    }

    /// <summary>
    /// Depo köküne göre göreli yol, her zaman <c>/</c> ile ayrılmış. <see cref="AllowedFiles"/>
    /// karşılaştırması bu normalizasyona dayanır — <c>Path.DirectorySeparatorChar</c> Windows'ta
    /// <c>\</c> olduğundan normalize edilmezse aynı dosya platforma göre farklı dizgeye çevrilir
    /// ve karşılaştırma sessizce kırılır.
    /// </summary>
    private static string Relative(string file) =>
        Path.GetRelativePath(RepoRoot(), file).Replace(Path.DirectorySeparatorChar, '/');

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx aranıyordu).");
    }
}
