using System.Text.RegularExpressions;
using MESNET.Internship.Core.Policies;
using MESNET.Internship.Core.ValueObjects;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Tıkanmışlık kararı ve o kararın LINQ ikizinin doğruluğu.
///
/// <para><b>Neden doğruluk tablosu:</b> Marten <c>IsCompleteOrOverridden()</c> metodunu SQL'e
/// çeviremez, bu yüzden sorgu koşulu bayrakları AÇARAK yazmak zorunda. Aynı karar iki yerde
/// yaşayınca ayrışabilir — ve ayrışma sessiz olur: kart yanlış sayı gösterir, hiçbir test
/// kırılmaz. Tablo o ayrışmayı imkânsız kılar.</para>
/// </summary>
public sealed class StuckApprovalPolicyTests
{
    private static readonly DateTime Now = new(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);

    private static TerminationApprovalChain Chain(
        bool teacher = false, bool deputy = false, bool director = false, bool overridden = false) =>
        new()
        {
            TeacherApproved = teacher,
            DeputyApproved = deputy,
            DirectorApproved = director,
            IsOverridden = overridden,
        };

    /// <summary>
    /// 16 bayrak birleşiminin HEPSİNDE, sorguda kullanılan açık ifade politikanın kendisiyle
    /// aynı şeyi söylemeli. Zincir kuralı bir gün değişirse (dördüncü onaycı) bu test kırmızı
    /// olur ve ayrışma sessiz kalmaz.
    /// </summary>
    [Fact]
    public void Acik_LINQ_ifadesi_politikayla_ayni_seyi_soyler()
    {
        var mismatches = new List<string>();

        foreach (var teacher in new[] { false, true })
        foreach (var deputy in new[] { false, true })
        foreach (var director in new[] { false, true })
        foreach (var overridden in new[] { false, true })
        {
            var chain = Chain(teacher, deputy, director, overridden);

            // GetStuckApprovalsHandler içindeki Where koşulunun birebir kopyası.
            var linq = !chain.IsOverridden
                       && !(chain.TeacherApproved && chain.DeputyApproved && chain.DirectorApproved);

            var policy = !chain.IsCompleteOrOverridden();

            if (linq != policy)
                mismatches.Add($"T={teacher} D={deputy} Dir={director} Ovr={overridden}: "
                               + $"linq={linq} policy={policy}");
        }

        mismatches.ShouldBeEmpty(
            "GetStuckApprovalsHandler'daki açık LINQ koşulu ile "
            + "TerminationApprovalChain.IsCompleteOrOverridden() ayrışmış. Sorgu koşulunu "
            + $"düzeltin. Ayrışmalar: {string.Join(" | ", mismatches)}");
    }

    [Fact]
    public void Zincir_yoksa_tikanmis_degildir()
    {
        StuckApprovalPolicy.IsStuck(null, requestedAt: null, Now, thresholdDays: 14)
            .ShouldBeFalse();
    }

    [Fact]
    public void Kapanmis_zincir_tikanmis_degildir()
    {
        var chain = Chain(teacher: true, deputy: true, director: true);
        StuckApprovalPolicy.IsStuck(chain, Now.AddDays(-100), Now, thresholdDays: 14)
            .ShouldBeFalse();
    }

    [Fact]
    public void Override_edilmis_zincir_tikanmis_degildir()
    {
        var chain = Chain(overridden: true);
        StuckApprovalPolicy.IsStuck(chain, Now.AddDays(-100), Now, thresholdDays: 14)
            .ShouldBeFalse();
    }

    [Fact]
    public void Esigin_altindaki_acik_zincir_tikanmis_degildir()
    {
        var chain = Chain(teacher: true);
        StuckApprovalPolicy.IsStuck(chain, Now.AddDays(-13), Now, thresholdDays: 14)
            .ShouldBeFalse();
    }

    [Fact]
    public void Esigi_asan_acik_zincir_tikanmistir()
    {
        var chain = Chain(teacher: true);
        StuckApprovalPolicy.IsStuck(chain, Now.AddDays(-15), Now, thresholdDays: 14)
            .ShouldBeTrue();
    }

    /// <summary>
    /// EKSİK VERİ SINIRI GEVŞETEMEZ (#252). Talep zamanı bilinmeyen açık zincir tıkanmış
    /// SAYILIR. Ters karar aylardır takılı duran eski kayıtları panodan sessizce silerdi —
    /// tam olarak kartın var olma sebebi olan durum.
    /// </summary>
    [Fact]
    public void Talep_zamani_bilinmeyen_acik_zincir_tikanmistir()
    {
        var chain = Chain(teacher: true);
        StuckApprovalPolicy.IsStuck(chain, requestedAt: null, Now, thresholdDays: 14)
            .ShouldBeTrue();
    }

    [Fact]
    public void Yas_gun_olarak_hesaplanir_bilinmiyorsa_null()
    {
        StuckApprovalPolicy.AgeInDays(Now.AddDays(-15), Now).ShouldBe(15);
        StuckApprovalPolicy.AgeInDays(null, Now).ShouldBeNull();
    }

    /// <summary>
    /// <see cref="Acik_LINQ_ifadesi_politikayla_ayni_seyi_soyler"/> yalnız KENDİ elle
    /// kopyalanmış ifadesini test eder — o metnin hâlâ <c>GetStuckApprovalsHandler</c>'daki
    /// gerçek <c>Where</c> koşuluyla aynı olduğuna dair HİÇBİR mekanik bağ yoktur. Biri
    /// handler'daki koşulu değiştirip yukarıdaki testin kopyasını unutursa, o test YEŞİL
    /// kalır ve ayrışma sessiz olur — testin kendi XML doc'unun "imkânsız kılar" dediği tam
    /// senaryo. Bu test kaynağı diskten okuyup handler'ın GERÇEK metnini tarar; el kopyası
    /// değil, kaynağa bağlı bir kilittir. Aynı fikir: <c>CrossTenantQueryDriftTests</c>,
    /// <c>TenantlessSessionDriftTests</c>, <c>InstitutionScopeDriftTests</c>.
    ///
    /// <para><b><c>StuckApprovalPolicy.IsStuck</c> neden hâlâ duruyor:</b> üretimde çağıranı
    /// yoktur (bilerek — üretim kararı Marten'in çevirebildiği LINQ'te yaşamak zorunda), ama
    /// <c>IsStuck</c> + doğruluk tablosu + bu kaynak kilidi ÜÇÜ BİRLİKTE ayrışmayı disiplin
    /// meselesi olmaktan çıkarıp mekanik hâle getirir — bu üçlüden birini "ölü kod" diye
    /// silmek kilidi kırar.</para>
    /// </summary>
    [Fact]
    public void GetStuckApprovalsHandler_Where_kosulu_kaynaktan_kilitlenir()
    {
        var handlerPath = Path.Combine(
            RepoRoot(), "src", "Modules", "Internship", "MESNET.Internship.Application",
            "Handlers", "GetStuckApprovalsHandler.cs");

        var source = CollapseWhitespace(StripComments(File.ReadAllText(handlerPath)));

        // GetStuckApprovalsHandler'daki Where koşulunun zincir bacağı — ApprovalChain != null,
        // !IsOverridden, üç onaycının olumsuzlanmış birleşik koşulu ve TerminationRequestedAt
        // disjunction'ı. Diskten okunan handler dosyasından BİREBİR kopyalandı.
        var expectedClause = CollapseWhitespace("""
            x.ApprovalChain != null
                        && !x.ApprovalChain.IsOverridden
                        && !(x.ApprovalChain.TeacherApproved
                             && x.ApprovalChain.DeputyApproved
                             && x.ApprovalChain.DirectorApproved)
                        && (x.TerminationRequestedAt == null
                            || x.TerminationRequestedAt <= cutoff)
            """);

        source.ShouldContain(expectedClause, Case.Sensitive,
            "GetStuckApprovalsHandler'daki Where koşulu değişmiş, ama buradaki beklenen ifade "
            + "güncellenmemiş — ikisi artık SESSİZCE ayrışmış olabilir. Hem buradaki "
            + "expectedClause dizgesini HEM DE Acik_LINQ_ifadesi_politikayla_ayni_seyi_soyler "
            + "içindeki linq ifadesini handler'daki yeni koşulla eşleşecek şekilde güncelleyin, "
            + "sonra tekrar koşun.");
    }

    /// <summary>
    /// Satır ve blok yorumlarını atar — handler'ın XML doc'ları bu koşulun adlarını (ör.
    /// <c>IsOverridden</c>) düz metinde geçirir; yorumu koda saymak yanlış-pozitif üretirdi.
    /// </summary>
    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//.*$", string.Empty, RegexOptions.Multiline);
    }

    /// <summary>
    /// Her boşluk dizisini TEK boşluğa indirger — yeniden biçimlendirmeye (satır sarma,
    /// girinti değişikliği) dayanıklı ama anlamsal değişikliğe dayanıksız bir karşılaştırma
    /// için.
    /// </summary>
    private static string CollapseWhitespace(string text) =>
        Regex.Replace(text, @"\s+", " ").Trim();

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
