using System.Text.RegularExpressions;
using MESNET.Common.Shared.Tenancy;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// <c>platform:tenant:manage</c> ile korunan bir uç, <b>kiracı damgalı</b> belgeye enjekte
/// edilmiş istek session'ıyla dokunamaz (#292).
///
/// <para><b>Çelişki nerede:</b> o izni taşıyan aktörün kurumu yoktur, dolayısıyla
/// <b>platform kiracısına</b> düşer. DI'dan gelen <c>IDocumentSession</c>/<c>IQuerySession</c>
/// istek kiracısına bağlıdır. Kiracı damgalı bir belgenin platform kiracısında <b>hiçbir satırı
/// yoktur</b> — sorgu boş döner, uç <b>200 verir</b> ve operatör işin yapıldığını sanar.</para>
///
/// <para><b>Ölçüldü:</b> <c>POST /api/internships/resync-sagas</c> ve
/// <c>POST /api/contracts/resync-internship-links</c> tam olarak bunu yapıyordu. Dev'de
/// görünmedi çünkü <c>admin</c> hesabı <c>InstitutionManager</c> ve <c>SystemAdmin</c>
/// rollerini <b>birlikte</b> taşıyor — kendi okulunun kiracısında koşuyordu.</para>
///
/// <para><b>Doğrusu:</b> <c>IDocumentStore</c> alın ve <c>ITenantDirectory</c> ile kiracı kiracı
/// dolaşın; her session'a kiracıyı <b>açıkça</b> verin. Olay yayınlıyorsanız
/// <c>DeliveryOptions.TenantId</c>'yi de verin — yoksa olay yayınlayanın (platform) kiracısını
/// devralır ve tüketici satırı yanlış kiracıda arar.</para>
///
/// <para><b>Neden "session enjekte etme" diye genel bir kural DEĞİL:</b> aynı izinle korunan üç
/// uç (<c>POST /api/institutions</c>, <c>/rebuild-hierarchy</c>, <c>/security/users/replay</c>)
/// yalnız <b>kimlik katmanı</b> belgelerine dokunur; onlar kiracı damgası taşımaz ve enjekte
/// session'la okunmaları doğrudur. Ayrımı yapan şey iznin kendisi değil,
/// <see cref="DocumentTenancyMap"/> sınıflandırmasıdır.</para>
/// </summary>
public sealed class PlatformScopedTenantDocumentDriftTests
{
    /// <summary>Kiracı-üstü izinle korunan uç kaydı — yöntem adını da yakalar.</summary>
    private static readonly Regex PlatformScopedRegistration = new(
        @"Map(?:Post|Get|Put|Delete)\s*\(\s*""([^""]*)""\s*,\s*(\w+)\s*\)[^;]*?Permissions\.Platform\.TenantManage",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex SessionParameter = new(
        @"\b(?:IDocumentSession|IQuerySession)\b", RegexOptions.Compiled);

    private static readonly Regex DocumentAccess = new(
        @"(?:Query|LoadAsync|LoadManyAsync)<(\w+)>", RegexOptions.Compiled);

    /// <summary>
    /// Uç yöntemi komut tipini gövdesinde <c>new X(...)</c> ile kurmuyorsa (gövdeden alıyorsa)
    /// tip ilk parametreden okunur. İkisi de tutmazsa zincir <b>çözülemedi</b> sayılır ve
    /// sessizce atlanmaz — <see cref="Kilit_gercek_zincir_cozuyor"/> bunu ölçer.
    /// </summary>
    private static readonly Regex FirstParameterType = new(@"^\s*(\w+)\s+\w+", RegexOptions.Compiled);

    [Fact]
    public void Platform_ucu_kiraci_belgesine_enjekte_sessionla_dokunamaz()
    {
        var violations = new List<string>();

        foreach (var chain in ResolveChains())
        {
            if (!chain.InjectsSession || chain.TenantDocuments.Count == 0)
                continue;

            violations.Add(
                $"{chain.Route} → {Path.GetFileName(chain.HandlerFile)} "
                + $"({string.Join(", ", chain.TenantDocuments)})");
        }

        violations.ShouldBeEmpty(
            "platform:tenant:manage ile korunan bir uç, kiracı damgalı belgeye enjekte edilmiş "
            + "istek session'ıyla dokunuyor. O izni taşıyan aktör platform kiracısına düşer ve "
            + "orada hiçbir satır yoktur: sorgu boş döner, uç 200 verir, operatör işin yapıldığını "
            + "sanar. IDocumentStore + ITenantDirectory ile kiracı kiracı dolaşın ve her session'a "
            + "kiracıyı açıkça verin; olay yayınlıyorsanız DeliveryOptions.TenantId'yi de verin. "
            + $"İhlaller: {string.Join(" | ", violations)}");
    }

    /// <summary>
    /// Tarama gerçekten zincir çözüyor mu? Çözemezse yukarıdaki test hiçbir şey kanıtlamadan
    /// yeşil kalırdı — uç kaydı biçimi değiştiğinde kilit sessizce boşa düşerdi.
    /// </summary>
    [Fact]
    public void Kilit_gercek_zincir_cozuyor()
    {
        var chains = ResolveChains();

        chains.ShouldNotBeEmpty(
            "Hiçbir platform:tenant:manage ucu çözülemedi. Uç kayıt biçimi ya da handler "
            + "adlandırması değişmiş olabilir; kilit bu hâliyle hiçbir şeyi korumuyor.");

        // Bugün beş uç var. Sayı düşerse ya uç silinmiştir ya da tarama onu artık görmüyordur;
        // ikisi de bakılmayı hak eder.
        chains.Count.ShouldBeGreaterThanOrEqualTo(5);

        // En az bir zincirin kiracı damgalı belgeye dokunması, sınıflandırma bacağının da
        // fiilen çalıştığını gösterir (DocumentTenancyMap okunabiliyor ve eşleşiyor).
        chains.ShouldContain(c => c.TenantDocuments.Count > 0);
    }

    private sealed record Chain(
        string Route, string HandlerFile, bool InjectsSession, IReadOnlyList<string> TenantDocuments);

    private static List<Chain> ResolveChains()
    {
        var tenantDocuments = DocumentTenancyMap.All
            .Where(pair => pair.Value == DocumentTenancy.Tenant)
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);

        var handlerFiles = Directory
            .EnumerateFiles(Path.Combine(RepoRoot(), "src", "Modules"), "*.cs", SearchOption.AllDirectories)
            .Where(f => f.Contains(".Application", StringComparison.Ordinal))
            .ToList();

        var chains = new List<Chain>();

        foreach (var endpointFile in EndpointFiles())
        {
            var source = File.ReadAllText(endpointFile);

            foreach (Match registration in PlatformScopedRegistration.Matches(source))
            {
                var route = registration.Groups[1].Value;
                var method = registration.Groups[2].Value;

                foreach (var command in CommandTypes(source, method))
                {
                    foreach (var handlerFile in handlerFiles)
                    {
                        var code = File.ReadAllText(handlerFile);
                        var signature = Regex.Match(code, @"Handle\(\s*" + command + @"\b([^)]*)\)", RegexOptions.Singleline);
                        if (!signature.Success)
                            continue;

                        var stripped = StripComments(code);
                        var documents = DocumentAccess.Matches(stripped)
                            .Select(m => m.Groups[1].Value)
                            .Where(tenantDocuments.Contains)
                            .Distinct(StringComparer.Ordinal)
                            .OrderBy(name => name, StringComparer.Ordinal)
                            .ToList();

                        chains.Add(new Chain(
                            route, handlerFile, SessionParameter.IsMatch(signature.Groups[1].Value), documents));
                    }
                }
            }
        }

        return chains;
    }

    /// <summary>
    /// Uç yönteminin devrettiği komut tip(ler)i: gövdedeki <c>new X(...)</c>, yoksa ilk
    /// parametrenin tipi (komut gövdeden bağlanıyorsa).
    /// </summary>
    private static IEnumerable<string> CommandTypes(string source, string method)
    {
        var body = Regex.Match(source, @"\b" + method + @"\s*\(([^)]*)\)\s*\{(.*?)\n    \}", RegexOptions.Singleline);
        if (!body.Success)
            return [];

        var constructed = Regex.Matches(body.Groups[2].Value, @"new\s+(\w+)\s*\(")
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (constructed.Count > 0)
            return constructed;

        var first = FirstParameterType.Match(body.Groups[1].Value);
        return first.Success ? [first.Groups[1].Value] : [];
    }

    private static IEnumerable<string> EndpointFiles() =>
        Directory
            .EnumerateFiles(Path.Combine(RepoRoot(), "src", "Modules"), "*Endpoints.cs", SearchOption.AllDirectories)
            .Where(f => f.Contains(".Api", StringComparison.Ordinal));

    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//.*$", string.Empty, RegexOptions.Multiline);
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

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx aranıyordu).");
    }
}
