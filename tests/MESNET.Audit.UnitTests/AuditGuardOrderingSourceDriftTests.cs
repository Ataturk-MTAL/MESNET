using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Madde 1 (KRİTİK) kilidi: <see cref="AuditGuardOrderingRegressionTests"/> kendi İZOLE
/// Wolverine ana bilgisayarını kurar ve sabit bir bayrakla (<c>DenetimGuarddanOnceKayitli</c>)
/// "doğru" sırayı taklit eder — <c>Program.cs</c>'i hiç OKUMAZ. Ölçüldü: <c>Program.cs</c>'teki
/// gerçek kayıt sırası guard'ların ALTINA alınıp geri alınsa dahi o test <b>43/43 yeşil kalırdı</b>
/// — hiçbir test projesi <c>MESNET.Presentation</c>'ı referans etmiyor, dolayısıyla o dosyayı
/// derleme zamanında ne çağıran ne de doğrulayan bir yol yok.
///
/// <para>Bu test kilidi <c>Program.cs</c>'in <b>kaynak metnini</b> tarar — bu deponun yerleşik
/// idiomu (<see cref="MESNET.Security.UnitTests.InstitutionScopeDriftTests"/>,
/// <see cref="MESNET.Security.UnitTests.TenantlessSessionDriftTests"/>,
/// <see cref="MESNET.Security.UnitTests.AnonymousEndpointDriftTests"/> hepsi aynı desende:
/// <c>MESNET.slnx</c>'i işaretçi kullanıp depo kökünü bulur, kaynak dosyayı düz metin olarak
/// okur, regex ile arar). <c>AuditGuardOrderingRegressionTests</c> SİLİNMEDİ — o, sıralamanın
/// DAVRANIŞINI (guard reddi denetim üstteyken iz bırakır) kilitliyor ve hâlâ değerli; bu test
/// onun tamamlayıcısıdır ve <c>Program.cs</c>'teki gerçek KAYIT SIRASINI kilitler.</para>
/// </summary>
public sealed class AuditGuardOrderingSourceDriftTests
{
    private const string ProgramCsRelativePath = "src/MESNET.Presentation/Program.cs";

    /// <summary>
    /// Denetim middleware kaydının tam nitelikli tip adı — kaynakta yalnız kayıt satırında
    /// <c>typeof(...)</c> içinde geçer; yorumdaki "AuditMiddleware.Before" ifadesiyle
    /// karışmaz çünkü aranan dize <c>typeof(</c> önekini de içerir.
    /// </summary>
    private const string AuditMiddlewareMarker =
        "typeof(MESNET.Audit.Application.Auditing.AuditMiddleware)";

    /// <summary>
    /// Guard <c>AddMiddleware(typeof(Tam.Nitelikli.AdGuardMiddleware))</c> kayıtları. Ad
    /// deseni (<c>...GuardMiddleware</c> soneki) bu deponun konvansiyonudur (bkz.
    /// <c>SalaryPeriodGuardMiddleware</c>, CLAUDE.md örneği) — yeni bir guard bu sonekle
    /// eklenirse tarama onu KENDİLİĞİNDEN yakalar, liste elle güncellenmez.
    /// </summary>
    private static readonly Regex GuardRegistration = new(
        @"AddMiddleware\(typeof\((?<type>[\w.]+GuardMiddleware)\)\)", RegexOptions.Compiled);

    [Fact]
    public void Denetim_kaydi_dort_guarddan_once_gelir()
    {
        var programCs = File.ReadAllText(Path.Combine(RepoRoot(), ProgramCsRelativePath));

        var auditIndex = programCs.IndexOf(AuditMiddlewareMarker, StringComparison.Ordinal);
        auditIndex.ShouldBeGreaterThanOrEqualTo(0,
            $"'{AuditMiddlewareMarker}' Program.cs'te bulunamadı — denetim middleware kaydı "
            + "kaldırılmış veya taşınmış olabilir. Bu test Program.cs'in kaynak metnini tarar.");

        var guardMatches = GuardRegistration.Matches(programCs);
        guardMatches.Count.ShouldBeGreaterThanOrEqualTo(4,
            "Program.cs'te 'GuardMiddleware' sonekli en az 4 AddMiddleware kaydı bekleniyordu "
            + $"(Payment/Contract/Attendance/Institution), {guardMatches.Count} bulundu. Guard "
            + "kayıtları kaldırılmış veya isimlendirme konvansiyonu (...GuardMiddleware) bozulmuş "
            + "olabilir.");

        var guardsBeforeAudit = guardMatches
            .Where(m => m.Index < auditIndex)
            .Select(m => m.Groups["type"].Value)
            .ToList();

        guardsBeforeAudit.ShouldBeEmpty(
            "Program.cs'te denetim middleware kaydı ('AuditMiddleware') şu guard "
            + "politikalarından SONRA kayıtlı: " + string.Join(", ", guardsBeforeAudit) + ". "
            + "Wolverine middleware zincirinde ilk kaydedilen EN DIŞTA sarar; denetim guard'ların "
            + "altında kayıtlıyken guard'ın reddettiği (DomainException) komut hiçbir iz satırı "
            + "bırakmıyor (ölçüldü: AuditMiddleware.Before hiç koşmuyor, accessor.Current null "
            + "kalıyor). Denetim kaydını dört guard kaydının ÜSTÜNE taşıyın.");
    }

    /// <summary>
    /// Test derlemesi depo içinde değil <c>bin/</c> altında koşar; göreli yol doğrudan
    /// kullanılamaz — çözüm dosyası (<c>MESNET.slnx</c>) işaretçi olarak aranır. Diğer drift
    /// testleriyle (<see cref="MESNET.Security.UnitTests.InstitutionScopeDriftTests"/> vb.)
    /// birebir aynı desen.
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
}
