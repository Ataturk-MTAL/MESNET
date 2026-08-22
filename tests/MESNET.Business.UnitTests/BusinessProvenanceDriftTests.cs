using Shouldly;
using Xunit;

namespace MESNET.Business.UnitTests;

/// <summary>
/// <c>Business.RegisteredByInstitutionId</c> <b>provenance</b>'tır — kapsam filtresi değil
/// (ADR-0003 adım 4).
///
/// <para><b>Neden kilitleniyor:</b> işletme kataloğu <b>paylaşımlıdır</b>
/// (<c>DocumentTenancyMap</c> → <c>Shared</c>): bir işletme birden çok okuldan öğrenci alır
/// ve tüm okullar tüm işletmeleri listeler. Alan bir filtreye girdiği anda katalog sessizce
/// okula bölünür — hata vermez, yalnız bazı işletmeler listede görünmez olur.</para>
///
/// <para><b>Neden liste:</b> alanın okuyucusu az olmalı ve <b>yeni bir okuyucu bir karar</b>
/// olmalıdır, kazara eklenen bir satır değil. Test yeni okuyucuyu kırmızıya çevirir; kararı
/// veren kişi listeye ekleyip gerekçesini yazar.</para>
/// </summary>
public sealed class BusinessProvenanceDriftTests
{
    private const string Field = "RegisteredByInstitutionId";

    /// <summary>
    /// Alanı okumasına/yazmasına izin verilen dosyalar. Hepsi ya kaydı üretir ya da
    /// provenance'ı olayla taşır; hiçbiri onunla <b>filtreleme</b> yapmaz.
    /// </summary>
    private static readonly HashSet<string> AllowedFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        // Alanın kendisi
        "Business.cs",
        // Provenance'ı taşıyan olaylar
        "BusinessRegistered.cs",
        "BusinessApproved.cs",
        "BusinessActivated.cs",
        // Kaydı üreten / durum geçişinde olayı yayınlayan handler'lar
        "RegisterBusinessHandler.cs",
        "SelfRegisterBusinessHandler.cs",
        "ApproveBusinessHandler.cs",
        "ActivateBusinessHandler.cs",
        // Provenance'ı bugünkü tek kurumlu kapsama çeviren TEK yer + çağıranları
        "BusinessScopeOrigin.cs",
        "BusinessRegisteredCoordinationConsumer.cs",
        "BusinessApprovedCoordinationConsumer.cs",
        "BusinessActivatedCoordinationConsumer.cs",
    };

    [Fact]
    public void Provenance_alaninin_okuyuculari_listeyle_sinirli()
    {
        var sourceRoot = Path.Combine(RepoRoot(), "src");
        Directory.Exists(sourceRoot).ShouldBeTrue($"Kaynak klasörü bulunamadı: {sourceRoot}");

        var unexpected = new List<string>();
        var seenAllowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].TrimStart();

                // Yorumlar serbest — alanın NEDEN kapsam olmadığını anlatan açıklamalar var.
                if (trimmed.StartsWith("//", StringComparison.Ordinal)
                    || trimmed.StartsWith("///", StringComparison.Ordinal))
                    continue;

                if (!lines[i].Contains(Field, StringComparison.Ordinal)) continue;

                var name = Path.GetFileName(file);
                if (AllowedFiles.Contains(name))
                {
                    seenAllowed.Add(name);
                    continue;
                }

                unexpected.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{i + 1}");
            }
        }

        unexpected.ShouldBeEmpty(
            $"'{Field}' provenance'tır (hangi okul kaydetti), kapsam DEĞİL. İşletme kataloğu "
            + "paylaşımlıdır: bir işletme birden çok okuldan öğrenci alır. Bu alanla filtre "
            + "yazmak kataloğu sessizce okula böler. Kapsam gerekiyorsa ilişkiden (yerleştirme) "
            + "türetin. Yeni bir okuyucu bilinçli bir kararsa AllowedFiles'a gerekçesiyle "
            + $"ekleyin (ADR-0003 adım 4).\n  {string.Join("\n  ", unexpected)}");

        // Tarama gerçekten çalıştı mı: "hiç ihlal yok" ile "hiç bakmadım" ayrı şeylerdir.
        seenAllowed.ShouldContain("Business.cs",
            "Tarama alanı hiç görmediyse bu test hiçbir şey doğrulamıyor demektir.");
    }

    /// <summary>
    /// Eski ad geri gelmemeli: <c>Business</c> entity'sinde <c>InstitutionId</c> adında bir
    /// alan, "bu işletme şu okula ait" okumasını geri getirir.
    /// </summary>
    [Fact]
    public void Business_entitysinde_InstitutionId_adinda_alan_yok()
    {
        var entity = Path.Combine(
            RepoRoot(), "src", "Modules", "Business", "MESNET.Business.Core", "Entities", "Business.cs");
        File.Exists(entity).ShouldBeTrue($"Entity bulunamadı: {entity}");

        var lines = File.ReadAllLines(entity);
        var offenders = lines
            .Select((line, index) => (line, index))
            .Where(x => !x.line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                     && !x.line.TrimStart().StartsWith("///", StringComparison.Ordinal))
            .Where(x => x.line.Contains("public Guid InstitutionId", StringComparison.Ordinal))
            .Select(x => $"Business.cs:{x.index + 1}")
            .ToList();

        offenders.ShouldBeEmpty(
            "Alan adı 'InstitutionId' olduğunda kapsam gibi okunur ve conjoined kiracılık "
            + "açıldığında paylaşımlı katalog okula bölünür. Doğru ad: RegisteredByInstitutionId.");
    }

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
}
