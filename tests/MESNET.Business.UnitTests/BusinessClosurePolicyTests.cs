using MESNET.Business.Core.Services;
using MESNET.Business.Core.ValueObjects;
using Shouldly;
using Xunit;

namespace MESNET.Business.UnitTests;

/// <summary>
/// Kapatma yeter sayısı (#151).
///
/// <para>İşletme kataloğu okullar arası paylaşımlıdır: bir okulun "kapandı" kararı <b>bütün
/// okulları</b> etkiler. Bu yüzden karar da birden çok okuldan gelmelidir.</para>
/// </summary>
public sealed class BusinessClosurePolicyTests
{
    private static readonly Guid OkulA = Guid.Parse("efd57b88-2f47-471c-9f51-476f80fabfca");
    private static readonly Guid OkulB = Guid.Parse("a24ebbab-8c58-4373-b936-640fa3247e77");

    private static BusinessClosureReport Rapor(Guid institutionId, Guid? reportedBy = null) =>
        new() { InstitutionId = institutionId, ReportedById = reportedBy ?? Guid.NewGuid() };

    /// <summary>
    /// <b>Kuralın kalbi.</b> Sayım farklı KURUM üzerinden yapılır; aynı okuldan iki yetkili
    /// sayılsaydı tek okul kendi başına küresel kapatma yapabilir ve yeter sayı boşa çıkardı.
    /// </summary>
    [Fact]
    public void Ayni_okuldan_iki_bildirim_bir_sayilir()
    {
        var reports = new[] { Rapor(OkulA), Rapor(OkulA) };

        BusinessClosurePolicy.DistinctReportingInstitutions(reports).ShouldBe(1);
        BusinessClosurePolicy.ReachesQuorum(reports, quorum: 2).ShouldBeFalse(
            "Tek okul iki yetkiliyle küresel kapatma yapamamalı.");
    }

    [Fact]
    public void Farkli_iki_okul_yeter_sayiyi_doldurur()
    {
        var reports = new[] { Rapor(OkulA), Rapor(OkulB) };

        BusinessClosurePolicy.ReachesQuorum(reports, quorum: 2).ShouldBeTrue();
    }

    /// <summary>
    /// Faz 1 davranışı değişmemeli: tek okul çalışırken yeter sayı 1'dir ve tek bildirim
    /// kapatır. Mekanizma yerinde, anahtar yapılandırmada.
    /// </summary>
    [Fact]
    public void Faz1_varsayilaninda_tek_bildirim_kapatir()
    {
        BusinessClosurePolicy.DefaultQuorum.ShouldBe(1);
        BusinessClosurePolicy
            .ReachesQuorum([Rapor(OkulA)], BusinessClosurePolicy.DefaultQuorum)
            .ShouldBeTrue();
    }

    [Fact]
    public void Bildirim_yoksa_kapanmaz()
    {
        BusinessClosurePolicy.ReachesQuorum([], quorum: 1).ShouldBeFalse();
    }

    /// <summary>
    /// Yapılandırmadan 0 ya da negatif gelirse "hiç bildirim olmadan kapalı" anlamına gelir ve
    /// <b>bütün katalog kapanırdı</b>. Eşik en az 1 kabul edilir.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Bozuk_esik_butun_katalogu_kapatmaz(int quorum)
    {
        BusinessClosurePolicy.ReachesQuorum([], quorum).ShouldBeFalse();
        BusinessClosurePolicy.ReachesQuorum([Rapor(OkulA)], quorum).ShouldBeTrue();
    }

    /// <summary>
    /// Bir okul yalnız <b>kendi</b> bildirimini geri çekebilir. Başkasınınkini kaldırabilseydi
    /// iki okulun kararını üçüncü okul tek başına bozardı.
    /// </summary>
    [Fact]
    public void Okul_yalniz_kendi_bildirimini_geri_ceker()
    {
        var raporA = Rapor(OkulA);

        BusinessClosurePolicy.CanRetract(raporA, OkulA).ShouldBeTrue();
        BusinessClosurePolicy.CanRetract(raporA, OkulB).ShouldBeFalse();
    }

    [Fact]
    public void Kapsamsiz_aktor_geri_cekemez()
    {
        BusinessClosurePolicy.CanRetract(Rapor(OkulA), Guid.Empty).ShouldBeFalse();
    }

    /// <summary>
    /// Geri çekme sayıyı düşürür; eşiğin altına inince işletme kendiliğinden açılır. Durum
    /// bildirimden bağımsız tutulsaydı bu otomatik geri dönüş mümkün olmazdı.
    /// </summary>
    [Fact]
    public void Geri_cekme_yeter_sayiyi_dusurur()
    {
        var reports = new List<BusinessClosureReport> { Rapor(OkulA), Rapor(OkulB) };
        BusinessClosurePolicy.ReachesQuorum(reports, quorum: 2).ShouldBeTrue();

        reports.RemoveAll(r => r.InstitutionId == OkulB);

        BusinessClosurePolicy.ReachesQuorum(reports, quorum: 2).ShouldBeFalse();
    }
}
