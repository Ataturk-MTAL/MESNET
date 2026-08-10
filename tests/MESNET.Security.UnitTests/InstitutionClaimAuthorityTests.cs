using System.Security.Claims;
using MESNET.Common.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Token'dan gelen <c>institution_id</c> <b>hiçbir koşulda</b> kabul edilmez
/// (ADR-0003 adım 2).
///
/// <para><b>Neden gerçek sınıf test ediliyor:</b> aynı sözleşmenin alan (branş) tarafı
/// (<c>BranchClaimSpoofingTests</c>) davranışı <i>yeniden yazılmış saf bir model</i> üzerinden
/// doğruluyor. O yaklaşım kuralın kendisini kilitler ama <b>kodun o kurala uyduğunu</b>
/// kilitlemez — <c>PermissionClaimsTransformation</c> içindeki sıra bozulsa test yine yeşil
/// kalırdı. Burada gerçek dönüşüm çağrılıyor.</para>
///
/// <para><b>Neden kayıt boşken bile atılıyor:</b> <c>institution_id</c> kiracı anahtarıdır.
/// "Sunucuda kaynak yoksa token'a düş" davranışı, kaydı olmayan bir kullanıcıya kendi
/// kiracısını seçtirirdi — Keycloak'ta bu öznitelik <i>unmanaged</i>'dır ve realm politikası
/// yanlış kurulursa kullanıcı kendi <c>manage-account</c> rolüyle onu yazabilir. Kapsamsız
/// kalmak, yanlış kiracıya düşmekten iyidir.</para>
/// </summary>
public sealed class InstitutionClaimAuthorityTests
{
    private const string Sub = "c8615312-8412-47d6-9d45-3e7d7b865c44";
    private static readonly Guid Spoofed = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Recorded = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task Kayit_bos_olsa_bile_tokendaki_kurum_atilir()
    {
        var transformation = Create(provider: null);

        var result = await transformation.TransformAsync(
            Principal(tokenInstitutionId: Spoofed));

        result.FindFirst("institution_id").ShouldBeNull(
            "Kayıt boşken token'a düşülürse kullanıcı kendi kiracısını seçebilir.");
    }

    [Fact]
    public async Task Kayit_doluysa_tokendaki_kurum_yerine_kayit_gecerlidir()
    {
        var transformation = Create(new StubProvider(Recorded));

        var result = await transformation.TransformAsync(
            Principal(tokenInstitutionId: Spoofed));

        result.FindAll("institution_id").Select(c => c.Value)
            .ShouldBe([Recorded.ToString()]);
    }

    /// <summary>
    /// Token'da hiç claim yokken kayıttaki kurum claim olarak eklenir — kapsamın normal yolu.
    /// </summary>
    [Fact]
    public async Task Tokende_claim_yokken_kayittaki_kurum_eklenir()
    {
        var transformation = Create(new StubProvider(Recorded));

        var result = await transformation.TransformAsync(Principal(tokenInstitutionId: null));

        result.FindFirst("institution_id")?.Value.ShouldBe(Recorded.ToString());
    }

    /// <summary>
    /// <b>Birden çok identity tuzağı.</b> Okuma tarafı <c>ClaimsPrincipal.FindFirst</c>
    /// kullanır ve bütün identity'lerdeki claim'leri görür; yalnız birincil identity
    /// temizlenseydi sahte değer okumada hayatta kalırdı.
    /// </summary>
    [Fact]
    public async Task Ikinci_identity_uzerindeki_sahte_kurum_da_atilir()
    {
        var transformation = Create(provider: null);

        var principal = Principal(tokenInstitutionId: null);
        principal.AddIdentity(new ClaimsIdentity(
            [new Claim("institution_id", Spoofed.ToString())], "second"));

        var result = await transformation.TransformAsync(principal);

        result.FindFirst("institution_id").ShouldBeNull();
    }

    /// <summary>
    /// <b>Kapı kapalı kalmalı:</b> hiçbir kod Keycloak'a <c>institution_id</c> özniteliği
    /// yazmamalıdır (ADR-0003 adım 2).
    ///
    /// <para><b>Neden dosya taraması:</b> davranış testi yalnız bugünkü çağrı yerlerini korur.
    /// Yarın bir handler "kullanıcı Keycloak konsolunda da görünsün" diye özniteliği geri
    /// yazarsa hiçbir birim testi görmez — ve o kopya, bir sonraki kişi için yeniden
    /// "otorite gibi görünen" bir kaynak olur. Kapatılan yol tam olarak buydu.</para>
    ///
    /// <para>Yasak <b>yazma</b> içindir. Okuma serbesttir: mevcut realm'lerde öznitelik
    /// duruyor ve yönetim ekranları onu gösterebilir.</para>
    /// </summary>
    [Fact]
    public void Hicbir_kod_keycloaka_institution_id_oznitelig_yazmaz()
    {
        var sourceRoot = Path.Combine(RepoRoot(), "src");
        Directory.Exists(sourceRoot).ShouldBeTrue($"Kaynak klasörü bulunamadı: {sourceRoot}");

        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.TrimStart();

                // Yorumlar serbest — kararın NEDEN böyle olduğunu anlatan açıklamalar var.
                if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;

                // Aranan: sözlük anahtarına ATAMA — ["institution_id"] = ...
                var key = line.IndexOf("[\"institution_id\"]", StringComparison.Ordinal);
                if (key < 0) continue;
                if (line.IndexOf('=', key) < 0) continue;

                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{i + 1}");
            }
        }

        violations.ShouldBeEmpty(
            "Kiracı anahtarı Keycloak'a yazılmaz (ADR-0003 adım 2). Otorite "
            + "UserAccount.InstitutionId'dir; Keycloak'taki bir kopya, ileride birinin onu "
            + "yeniden otorite sanmasına davetiye çıkarır. Kurum bağı yalnız "
            + "ChangeUserInstitution ile değişir.\n  " + string.Join("\n  ", violations));
    }

    // ─── Kurulum ────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Test derlemesi depo içinde değil <c>bin/</c> altında koşar; göreli yol doğrudan
    /// kullanılamaz — çözüm dosyası (<c>MESNET.slnx</c>) işaretçi olarak aranır.
    /// </summary>
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

    /// <summary>
    /// Marten kaydı ve <c>IDocumentStore</c> olmayan bir kapsayıcı: dönüşüm ikisini de
    /// zarifçe atlar, yani testte kalan tek kurum kaynağı token'dır. Kural tam olarak
    /// budur — o kaynak kabul edilmemeli.
    /// </summary>
    private static PermissionClaimsTransformation Create(IUserPermissionProvider? provider)
    {
        var services = new ServiceCollection();
        if (provider is not null)
            services.AddSingleton(provider);

        return new PermissionClaimsTransformation(
            services.BuildServiceProvider(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PermissionClaimsTransformation>.Instance);
    }

    private static ClaimsPrincipal Principal(Guid? tokenInstitutionId)
    {
        var claims = new List<Claim> { new("sub", Sub) };
        if (tokenInstitutionId is { } id)
            claims.Add(new Claim("institution_id", id.ToString()));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private sealed class StubProvider(Guid institutionId) : IUserPermissionProvider
    {
        public Task<UserPermissionInfo?> GetUserPermissionInfoAsync(string keycloakUserId) =>
            Task.FromResult<UserPermissionInfo?>(new UserPermissionInfo(
                IsEnabled: true,
                Roles: ["Teacher"],
                DirectPermissions: [],
                BranchCodes: [],
                InstitutionId: institutionId));
    }
}
