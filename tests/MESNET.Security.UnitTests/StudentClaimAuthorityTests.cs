using System.Security.Claims;
using MESNET.Common.Infrastructure.Security;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Token'dan gelen <c>student_id</c> <b>hiçbir koşulda</b> kabul edilmez (#230).
///
/// <para><b>Bu alan bir yetki kapsamıdır.</b> <c>PaidLeaveApprovalPolicy.CanBusinessApprove</c>
/// yalnız buna bakar (#177) ve arkadan yakalayan başka kontrol yoktur; dönem notu girişi ile
/// <c>internship:view-own</c> işletme basamağı da aynı claim'e dayanır. Kullanıcı kendi
/// değerini belirleyebilseydi <b>başka işletmenin</b> ücretli izin talebini onaylar ve
/// <b>başka işletmenin</b> öğrencisine not girerdi.</para>
///
/// <para><b>Kiracılık yarıçapı daraltır, kapatmaz.</b> <c>PaidLeaveRequest</c> ve
/// <c>StudentTermGrade</c> kiracıya aittir, dolayısıyla sahte bir değer başka <i>okulun</i>
/// verisine ulaşamaz. Ama aynı okuldaki <b>tüm işletmeler</b> açık kalırdı — bir okulda onlarca
/// işletme olacağı için bu yeterli bir savunma değildir.</para>
///
/// <para><b>Neden gerçek sınıf test ediliyor:</b> saf bir model üzerinden doğrulamak kuralı
/// kilitler ama <b>kodun o kurala uyduğunu</b> kilitlemez — dönüşümdeki sıra bozulsa test yine
/// yeşil kalırdı. Burada gerçek dönüşüm çağrılıyor. Aynı gerekçe
/// <c>InstitutionClaimAuthorityTests</c>'te de yazılı.</para>
/// </summary>
public sealed class StudentClaimAuthorityTests
{
    private const string Sub = "bf3c4841-d3a4-4c91-99ba-3ef579282831";
    private static readonly Guid Spoofed = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Recorded = Guid.Parse("0bdbaf37-3cad-4ba0-9ba1-ef88eb9c2615");

    /// <summary>
    /// Kayıt boşken token'a düşülürse kullanıcı <b>kendi işletmesini seçer</b>. Kapsamsız
    /// kalmak, yanlış işletmeye düşmekten iyidir.
    /// </summary>
    [Fact]
    public async Task Kayit_bos_olsa_bile_tokendaki_ogrenci_atilir()
    {
        var transformation = Create(provider: null);

        var result = await transformation.TransformAsync(Principal(tokenStudentId: Spoofed));

        result.FindFirst("student_id").ShouldBeNull();
    }

    [Fact]
    public async Task Kayit_doluysa_tokendaki_ogrenci_yerine_kayit_gecerlidir()
    {
        var transformation = Create(new StubStudentProvider(Recorded));

        var result = await transformation.TransformAsync(Principal(tokenStudentId: Spoofed));

        result.FindAll("student_id").Select(c => c.Value).ShouldBe([Recorded.ToString()]);
    }

    [Fact]
    public async Task Tokende_claim_yokken_kayittaki_ogrenci_eklenir()
    {
        var transformation = Create(new StubStudentProvider(Recorded));

        var result = await transformation.TransformAsync(Principal(tokenStudentId: null));

        result.FindFirst("student_id")?.Value.ShouldBe(Recorded.ToString());
    }

    /// <summary>
    /// <b>Birden çok identity tuzağı.</b> Okuma tarafı <c>ClaimsPrincipal.FindFirst</c> kullanır
    /// ve bütün identity'lerdeki claim'leri görür; yalnız birincil identity temizlenseydi sahte
    /// değer okumada hayatta kalırdı.
    /// </summary>
    [Fact]
    public async Task Ikinci_identity_uzerindeki_sahte_ogrenci_de_atilir()
    {
        var transformation = Create(provider: null);

        var principal = Principal(tokenStudentId: null);
        principal.AddIdentity(new ClaimsIdentity(
            [new Claim("student_id", Spoofed.ToString())], "second"));

        var result = await transformation.TransformAsync(principal);

        result.FindFirst("student_id").ShouldBeNull();
    }

    /// <summary>
    /// <b>Kapı kapalı kalmalı:</b> hiçbir kod Keycloak'a <c>student_id</c> özniteliği
    /// yazmamalıdır.
    ///
    /// <para>Davranış testi yalnız bugünkü çağrı yerlerini korur. Yarın bir handler "kullanıcı
    /// Keycloak konsolunda da görünsün" diye özniteliği geri yazarsa hiçbir birim testi görmez —
    /// ve o kopya, bir sonraki kişi için yeniden "otorite gibi görünen" bir kaynak olur.</para>
    ///
    /// <para>Yasak <b>yazma</b> içindir; okuma serbesttir (mevcut realm'lerde öznitelik duruyor).</para>
    /// </summary>
    [Fact]
    public void Hicbir_kod_keycloaka_student_id_ozniteligi_yazmaz()
    {
        var sourceRoot = Path.Combine(RepoRoot(), "src");
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal)) continue;

                var key = lines[i].IndexOf("[\"student_id\"]", StringComparison.Ordinal);
                if (key < 0) continue;
                if (lines[i].IndexOf('=', key) < 0) continue;

                violations.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{i + 1}");
            }
        }

        violations.ShouldBeEmpty(
            "Keycloak'a student_id özniteliği yazılıyor. Otorite UserAccount.BusinessId'dir; "
            + "Keycloak'taki kopya unmanaged özniteliktir ve realm politikası yanlış kurulursa "
            + $"kullanıcı onu kendi yazar. İhlaller: {string.Join(", ", violations)}");
    }

    private static PermissionClaimsTransformation Create(StubStudentProvider? provider)
    {
        var services = new ServiceCollection();
        if (provider is not null)
            services.AddScoped<IUserPermissionProvider>(_ => provider);

        return new PermissionClaimsTransformation(
            services.BuildServiceProvider(),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<PermissionClaimsTransformation>.Instance);
    }

    private static ClaimsPrincipal Principal(Guid? tokenStudentId)
    {
        var claims = new List<Claim> { new("sub", Sub) };
        if (tokenStudentId is { } id)
            claims.Add(new Claim("student_id", id.ToString()));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private sealed class StubStudentProvider(Guid studentId) : IUserPermissionProvider
    {
        public Task<UserPermissionInfo?> GetUserPermissionInfoAsync(string keycloakUserId) =>
            Task.FromResult<UserPermissionInfo?>(new UserPermissionInfo(
                IsEnabled: true,
                Roles: ["CompanyManager"],
                DirectPermissions: [],
                BranchCodes: [],
                InstitutionId: null,
                StudentId: studentId,
                LinkedStudentIds: [],
                RolesWrittenAt: DateTime.UtcNow));
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
