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
/// <para><b>Doğrusu:</b> kapsam <c>SubtreeTenantScope.ResolveAsync</c> ile
/// <c>InstitutionVisibility</c>'den türetilir; sorgu o listeyle
/// <c>TenantIsOneOf(tenants.ToArray())</c> çağırır.</para>
/// </summary>
public sealed class CrossTenantQueryDriftTests
{
    /// <summary>Kapsamı tümden kaldıran operatör — hiçbir gerekçeyle kullanılmaz.</summary>
    private static readonly Regex AnyTenantCall = new(@"\bAnyTenant\s*\(", RegexOptions.Compiled);

    /// <summary>Kapsamı listeye daraltan operatör — yalnız izinli dosyalarda.</summary>
    private static readonly Regex TenantIsOneOfCall =
        new(@"\bTenantIsOneOf\s*\(", RegexOptions.Compiled);

    /// <summary>
    /// Operatörü kullanabilecek tek üretim dosyaları — depo köküne göre TAM YOL. Yalnız dosya
    /// adını karşılaştırmak, başka bir yerde aynı adı taşıyan bir dosyanın (ör. başka bir modülde
    /// yeniden yazılmış bir <c>SubtreeTenantScope.cs</c>) sessizce izinli sayılmasına yol açardı
    /// ve tek kapı garantisini delerdi. Karşılaştırma <see cref="Relative"/>'in ürettiği, her
    /// zaman <c>/</c> ile ayrılmış göreli yol üzerinden yapılır. Sorgu handler'ı listeyi buradan
    /// alır ama operatörü kendisi çağırır; bu yüzden handler dosyası da izinlidir.
    /// </summary>
    private static readonly string[] AllowedFiles =
    [
        "src/MESNET.Common.Infrastructure/Tenancy/SubtreeTenantScope.cs",
        "src/Modules/Internship/MESNET.Internship.Application/Handlers/GetStuckApprovalsHandler.cs",
    ];

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
    public void TenantIsOneOf_yalniz_izinli_dosyalarda()
    {
        var violations = new List<string>();

        foreach (var file in SourceFiles())
        {
            var code = StripComments(File.ReadAllText(file));
            if (!TenantIsOneOfCall.IsMatch(code))
                continue;

            if (!AllowedFiles.Contains(Relative(file), StringComparer.Ordinal))
                violations.Add(Relative(file));
        }

        violations.ShouldBeEmpty(
            "TenantIsOneOf(...) kiracı yalıtımını deler ve yalnız tek kapıdan kullanılır. "
            + "Kapsamı SubtreeTenantScope.ResolveAsync'ten alın; listeyi istekten ALMAYIN. "
            + $"İhlaller: {string.Join(" | ", violations)}");
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

    private static IEnumerable<string> SourceFiles()
    {
        var root = Path.Combine(RepoRoot(), "src");
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal));
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
