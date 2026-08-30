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
}
