using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kiracısız Marten session'ı açılmamalı (#149).
///
/// <para><b>Neden bu kilit var — gerçekten yaşandı:</b> kiracılık kapısı açıldığında
/// (<c>Advanced.DefaultTenantUsageEnabled = false</c>) DI'dan gelen <c>IQuerySession</c>
/// kiracısız kaldı ve <c>UserPermissionProvider</c> sorgusu
/// <c>DefaultTenantUsageDisabledException</c> fırlattı. İstisna
/// <c>PermissionClaimsTransformation</c> içinde yutuluyordu; sonuç sessizdi ve <b>tam ters
/// yöndeydi</b>: yetkilendirme token'daki rollere geri düştü — ADR-0003 adım 2'nin kapattığı
/// yol. Ölçüldü: DEVRE DIŞI bırakılmış bir hesap 22 izinle öğrenci verisi okumaya devam
/// ediyordu (HTTP 200).</para>
///
/// <para><b>Neden derleyici yakalayamaz:</b> <c>store.QuerySession()</c> geçerli bir çağrıdır
/// ve yalnız <i>çalışma zamanında</i> patlar. Patladığı yer de çoğu zaman bir <c>catch</c>
/// bloğunun içidir — yani hatanın kendisi bile görünmez. Tek savunma, çağrının kaynakta hiç
/// bulunmamasıdır.</para>
///
/// <para><b>Doğrusu:</b> istek bağlamında kiracı <c>TenantResolutionMiddleware</c> ile
/// <c>IMessageBus.TenantId</c> üzerine konur ve handler'ların session'ları onu devralır. İstek
/// bağlamı dışında (arka plan işi, kimlik katmanı) kiracı <b>açıkça</b> verilir:
/// <c>store.QuerySession(tenantId)</c> ya da hiçbir okula ait olmayan işler için
/// <c>store.QuerySession(TenantResolution.Platform)</c>.</para>
/// </summary>
public sealed class TenantlessSessionDriftTests
{
    /// <summary>
    /// Argümansız session açma çağrıları. Argümanlı sürümler (<c>QuerySession(tenantId)</c>,
    /// <c>LightweightSession(SessionOptions)</c>) bilinçli karardır ve eşleşmez.
    /// </summary>
    private static readonly Regex TenantlessOpen = new(
        @"\.(QuerySession|LightweightSession|IdentitySession|OpenSession|DirtyTrackedSession)\(\s*\)",
        RegexOptions.Compiled);

    /// <summary>
    /// İstek bağlamı DIŞINDA çalışan sınıflar: kiracıyı istekten devralamazlar, dolayısıyla
    /// DI'dan gelen (kiracısız) session'ı enjekte edemezler.
    /// </summary>
    private static readonly string[] OutsideRequestContext =
    [
        "BackgroundService",
        "IHostedService",
        "IClaimsTransformation",
        "IUserPermissionProvider",
    ];

    private static readonly Regex InjectedSession = new(
        @"\b(IQuerySession|IDocumentSession)\b", RegexOptions.Compiled);

    [Fact]
    public void Kaynakta_argumansiz_session_acma_yok()
    {
        var violations = new List<string>();

        foreach (var file in SourceFiles())
        {
            var code = StripComments(File.ReadAllText(file));
            foreach (Match match in TenantlessOpen.Matches(code))
            {
                violations.Add($"{Relative(file)}: {match.Value.Trim()}");
            }
        }

        violations.ShouldBeEmpty(
            "Kiracısız session açılıyor. Kapı açıkken bu çağrı DefaultTenantUsageDisabledException "
            + "fırlatır ve istisna bir catch bloğunda yutulursa hata SESSİZ kalır. Kiracıyı açıkça "
            + "verin: store.QuerySession(tenantId) — hiçbir okula ait olmayan iş için "
            + $"store.QuerySession(TenantResolution.Platform). İhlaller: {string.Join(" | ", violations)}");
    }

    [Fact]
    public void Istek_disinda_calisan_sinifa_session_enjekte_edilmez()
    {
        var violations = new List<string>();

        foreach (var file in SourceFiles())
        {
            // Yorumlar taranmaz: bu kuralın NEDENİNİ anlatan XML doc'lar tam da yasak tipin
            // adını geçirir. Yorumu koda saymak, doğru yazılmış dosyayı ihlal gösterirdi —
            // testin ilk sürümünde tam olarak bu oldu (UserPermissionProvider kendi
            // açıklamasıyla kırıldı).
            var code = StripComments(File.ReadAllText(file));

            if (!OutsideRequestContext.Any(marker => code.Contains(marker, StringComparison.Ordinal)))
                continue;

            if (InjectedSession.IsMatch(code))
                violations.Add(Relative(file));
        }

        violations.ShouldBeEmpty(
            "İstek bağlamı dışında çalışan bir sınıfa IQuerySession/IDocumentSession enjekte "
            + "edilmiş. DI'dan gelen session kiracısızdır; bu sınıflar kiracıyı istekten "
            + "devralamaz. IDocumentStore alıp kiracıyı açıkça verin (arka plan işleri için "
            + $"ITenantDirectory ile kiracı kiracı dolaşın). İhlaller: {string.Join(" | ", violations)}");
    }

    /// <summary>
    /// Satır yorumlarını (<c>//</c>, XML doc dahil) ve blok yorumlarını atar. Dize içindeki
    /// <c>//</c> dizileri de silinir; bu tarama için kabul edilebilir bir kayıptır — amaç
    /// tip adının <b>kodda</b> geçip geçmediğini anlamak, sözdizimini çözümlemek değil.
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

    private static string Relative(string file) =>
        Path.GetRelativePath(RepoRoot(), file);

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
