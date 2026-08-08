using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Fesih akışında <b>aktör token'dan damgalanır</b>, istemciden alınmaz (#191).
///
/// <para><b>Yaşanan:</b> <c>RequestTermination.RequestedBy</c> ve
/// <c>OverrideTerminationApproval.OverriddenBy</c> uçlara doğrudan bağlanıyordu — yani
/// "fesih talebini kim açtı" ve "onay zincirini kim atladı" sorularının yanıtı <b>istemcinin
/// gönderdiği metinden</b> yazılıyordu. Saga onu olduğu gibi kaydediyor
/// (<c>OverriddenBy = e.OverriddenBy</c>), dolayısıyla denetim izi taklit edilebilirdi.</para>
///
/// <para>Override, onay zincirini tümüyle geçersizleştiren tek işlemdir; izinin güvenilir
/// olması özellikle önemli.</para>
///
/// <para><b>Neden kaynak taraması:</b> düzeltme "gövdede alan yok" biçiminde yapısaldır ve
/// çalışma zamanında sınanması bir uçtan uca test gerektirir. Tarama ucuz ve kesin: komut
/// tipi yeniden gövdeye bağlanırsa test kırılır.</para>
/// </summary>
public sealed class TerminationActorStampingTests
{
    /// <summary>Uç metodunun gövdeden bağladığı tip — komut tipi olmamalı.</summary>
    private static readonly Regex KomutuDogrudanBaglama = new(
        @"^\s*Guid internshipId,\s*(RequestTermination|OverrideTerminationApproval)\s+\w+",
        RegexOptions.Compiled | RegexOptions.Multiline);

    [Fact]
    public void Uclar_aktor_alanini_istemciden_almaz()
    {
        var dosya = Path.Combine(
            RepoRoot(), "src", "Modules", "Internship", "MESNET.Internship.Api",
            "InternshipEndpoints.cs");

        File.Exists(dosya).ShouldBeTrue($"Uç dosyası bulunamadı: {dosya}");

        var eslesmeler = KomutuDogrudanBaglama.Matches(File.ReadAllText(dosya))
            .Select(m => m.Value.Trim())
            .ToList();

        eslesmeler.ShouldBeEmpty(
            "Fesih uçları komut tipini gövdeye bağlamamalı (#191): RequestedBy/OverriddenBy "
            + "istemciden gelirse denetim izi taklit edilebilir. Gövde için *Request kaydını "
            + "kullanın, aktörü ICurrentUserService'ten damgalayın:\n  "
            + string.Join("\n  ", eslesmeler));
    }

    /// <summary>
    /// Taramanın gerçekten çalıştığını doğrular — desen bozulursa test sessizce yeşil kalırdı.
    /// </summary>
    [Fact]
    public void Tarama_deseni_gecerli()
    {
        const string ornek = """
                private static async Task<IResult> PostOverride(
                    Guid internshipId, OverrideTerminationApproval command, IMessageBus bus)
            """;

        KomutuDogrudanBaglama.IsMatch(ornek).ShouldBeTrue(
            "Desen bilinen ihlali göremiyorsa test hiçbir şey doğrulamaz.");
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
