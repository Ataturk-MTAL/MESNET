using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kurum kimliğini <b>istekten</b> alan her command/query kapsam guard'ından geçmelidir
/// (ADR-0003 adım 6).
///
/// <para><b>Neden kilit gerekiyor:</b> kiracılık bu yüzeyi korumaz — <c>Institution</c> belgesi
/// kiracının kendisidir ve kiracı damgası taşımaz. Koruma tek tek uçlara bırakılırsa unutulan
/// biri <b>başka okulun kaydını açar</b> ve sonuç sessizdir: istek 200 döner, log temiz kalır.
/// Ölçüldü — kontrol yokken bir okul müdürü diğer okulun personel listesini okudu, adını
/// değiştirdi ve personel ekledi.</para>
///
/// <para>Kontrol mesaj tipine bağlıdır (<c>IInstitutionScoped</c> + Wolverine middleware), yani
/// yeni bir uç eklemek yetmez; mesajın arayüzü taşıması gerekir. Bu test o adımın unutulmadığını
/// doğrular.</para>
/// </summary>
public sealed class InstitutionScopeDriftTests
{
    private const string ApplicationPath = "Modules/Institution/MESNET.Institution.Application";

    /// <summary><c>Guid InstitutionId</c> alanı taşıyan kayıt bildirimleri.</summary>
    private static readonly Regex RecordDeclaration = new(
        @"public sealed record (?<name>\w+)\((?<body>[^)]*)\)(?<bases>[^;{]*)", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Kapsam guard'ından <b>bilinçli olarak</b> muaf tutulanlar.
    ///
    /// <para><c>CreateInstitution</c>: henüz var olmayan bir kurumu yaratır, karşılaştırılacak
    /// mevcut kapsam yoktur. Kontrol yerine <b>ucun izni</b> kurum üstüdür
    /// (<c>platform:tenant:manage</c>) — yeni okul açmak kurum içi bir iş değildir.</para>
    /// </summary>
    private static readonly HashSet<string> Exempt = new(StringComparer.Ordinal)
    {
        "CreateInstitution",
    };

    [Fact]
    public void Kurum_kimligi_tasiyan_mesajlar_guard_arayuzunu_tasir()
    {
        var missing = new List<string>();

        foreach (var file in SourceFiles())
        {
            var source = File.ReadAllText(file);

            foreach (Match match in RecordDeclaration.Matches(source))
            {
                var name = match.Groups["name"].Value;
                if (Exempt.Contains(name)) continue;

                // Yalnız kurum kimliğini İSTEKTEN alanlar; sonuç/DTO kayıtları elenir.
                if (!match.Groups["body"].Value.Contains("Guid InstitutionId", StringComparison.Ordinal))
                    continue;

                if (!match.Groups["bases"].Value.Contains("IInstitutionScoped", StringComparison.Ordinal))
                    missing.Add($"{name} ({Path.GetFileName(file)})");
            }
        }

        missing.ShouldBeEmpty(
            "Kurum kimliğini istekten alan mesaj IInstitutionScoped taşımıyor; kapsam guard'ı bu "
            + "mesaj için HİÇ çalışmaz ve aktör başka okulun verisine dokunabilir. Arayüzü ekleyin "
            + "ya da bilinçli bir istisnaysa testteki muafiyet listesine gerekçesiyle yazın. "
            + $"Eksikler: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// Bütün kurumları dolaşan kod. Guard bunu <b>göremez</b>: mesaj hiç kurum kimliği
    /// taşımadığı için <c>IInstitutionScoped</c> kuralına takılmaz.
    /// </summary>
    private static readonly Regex EnumeratesAllInstitutions = new(
        @"Query<(Core\.Entities\.)?Institution(Record)?>\(\)", RegexOptions.Compiled);

    /// <summary>
    /// Kurum belgesini <b>filtresiz</b> dolaşmasına izin verilen yerler.
    ///
    /// <para><c>GetInstitutionsHandler</c>: listeleme sorgusu; kapsam guard'la değil
    /// <c>VisibleInstitutionFilter</c> ile uygulanır (hedef istekte geçmez).
    /// <c>InstitutionTenantDirectory</c>: arka plan işlerinin kiracı listesi — kiracıları
    /// saymak bu servisin tanımıdır, kullanıcı isteğine bağlı değildir.</para>
    /// </summary>
    private static readonly HashSet<string> MayEnumerateAll = new(StringComparer.Ordinal)
    {
        "GetInstitutionsHandler.cs",
        "InstitutionTenantDirectory.cs",
    };

    /// <summary>
    /// <b>Guard'ın kör noktası.</b> Kimliği istekten alan mesajları guard kapatır; ama hiç
    /// kimlik almadan <b>bütün kurumları</b> dolaşan bir handler kuralın dışında kalır ve
    /// sessizce her okulun verisine dokunur.
    ///
    /// <para>Gerçekten yaşandı: <c>ResyncStaffBranchCodesHandler</c> tüm kurumları tarıyordu ve
    /// kodda "Faz 1 tek kurumlu olduğu için pratik etkisi yok" diyen bir TODO vardı. İkinci okul
    /// açılınca ölçüldü — kendi okulunda <b>1</b> personeli olan müdür ucu çağırdığında
    /// <b>9</b> personel işlendi ve olaylar başka okulların kullanıcılarına <b>kapsam yazdı</b>.</para>
    /// </summary>
    [Fact]
    public void Butun_kurumlari_dolasan_yeni_kod_eklenemez()
    {
        var offenders = new List<string>();

        foreach (var file in ApplicationFiles())
        {
            var name = Path.GetFileName(file);
            if (MayEnumerateAll.Contains(name)) continue;

            if (EnumeratesAllInstitutions.IsMatch(File.ReadAllText(file)))
                offenders.Add(name);
        }

        offenders.ShouldBeEmpty(
            "Kurum belgesi filtresiz dolaşılıyor. Bu kod hiçbir kurum kimliği almadığı için "
            + "kapsam guard'ı ONU HİÇ GÖRMEZ ve her okulun verisine dokunur. Komuta InstitutionId "
            + "ekleyip IInstitutionScoped yapın; gerçekten kiracı-üstü bir işse muafiyet listesine "
            + $"gerekçesiyle yazın. İhlaller: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// Muafiyet listesi <b>küçük kalmalı</b>. Büyümesi, guard'ın kural olmaktan çıkıp
    /// istisnalar tablosuna dönüştüğünün işaretidir.
    /// </summary>
    [Fact]
    public void Muafiyet_listesi_kucuk_kalir()
    {
        Exempt.Count.ShouldBeLessThanOrEqualTo(2);
    }

    /// <summary>
    /// Yalnız <b>istek</b> tipleri taranır: <c>Commands/</c> ve <c>Queries/</c>. DTO'lar da
    /// <c>Guid InstitutionId</c> taşır ama onlar isteğin girdisi değil ÇIKTISIDIR; kapsam
    /// kararına konu olmazlar. (İlk sürüm bütün klasörü tarıyordu ve
    /// <c>AcademicPeriodDto</c>'yu ihlal sayıyordu.)
    /// </summary>
    /// <summary>Modülün tamamı — handler'lar ve servisler dâhil.</summary>
    private static IEnumerable<string> ApplicationFiles()
    {
        var root = Path.Combine(RepoRoot(), "src", ApplicationPath);
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal));
    }

    private static IEnumerable<string> SourceFiles()
    {
        var root = Path.Combine(RepoRoot(), "src", ApplicationPath);
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";
        var requestFolders = new[]
        {
            $"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}",
            $"{Path.DirectorySeparatorChar}Queries{Path.DirectorySeparatorChar}",
        };

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal)
                     && requestFolders.Any(folder => f.Contains(folder, StringComparison.Ordinal)));
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
