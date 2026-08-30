using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.Services;
using Shouldly;
using Xunit;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Ağacı mevcut okul künyelerinden kuran geçiş kararı.
///
/// <para><b>Neden saf bir planlayıcı, neden handler'ın içinde değil:</b> bu geçişin tek
/// kritik özelliği <b>idempotanlık</b>tır — ikinci koşu aynı ağacı üretmeli, düğüm
/// ÇOĞALTMAMALIDIR. Mantık handler'ın içinde kalsaydı bunu ancak veritabanına iki kez
/// yazarak sınayabilirdik; burada iki kez plan üretip karşılaştırmak yeter.</para>
/// </summary>
public sealed class InstitutionHierarchyPlannerTests
{
    private static int _counter;

    /// <summary>Deterministik kimlik üreteci — plan iki kez koşturulduğunda karşılaştırılabilsin.</summary>
    private static Func<Guid> Ids()
    {
        var n = 0;
        return () => Guid.Parse($"{++n:D8}-0000-0000-0000-000000000000");
    }

    private static InstitutionRecord Okul(
        string ad, string? il = "06", string? ilce = "Yenimahalle", Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.Parse($"{++_counter:D8}-1111-1111-1111-111111111111"),
            InstitutionCode = 900000 + _counter,
            FullName = ad,
            ProvinceCode = il,
            DistrictName = ilce
        };

    [Fact]
    public void Bos_girdi_bos_plan_uretir()
    {
        var plan = InstitutionHierarchyPlanner.Plan([], Ids());

        plan.Created.ShouldBeEmpty();
        plan.Assignments.ShouldBeEmpty();
        plan.SkippedNoProvince.ShouldBeEmpty();
    }

    [Fact]
    public void Tek_okul_icin_il_ve_ilce_dugumu_uretilir()
    {
        var okul = Okul("Atatürk MTAL");

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        plan.Created.Count.ShouldBe(2);
        plan.Created.Count(c => c.NodeTypeName == InstitutionNodeType.Province.Name).ShouldBe(1);
        plan.Created.Count(c => c.NodeTypeName == InstitutionNodeType.District.Name).ShouldBe(1);
    }

    [Fact]
    public void Okulun_yolu_uc_segmentlidir_ve_ayracla_baslar_biter()
    {
        var okul = Okul("Atatürk MTAL");

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());
        var atama = plan.Assignments.Single(a => a.Id == okul.Id);

        atama.Path.ShouldStartWith("/");
        atama.Path.ShouldEndWith("/");
        atama.Path.Trim('/').Split('/').Length.ShouldBe(3);
        atama.NodeTypeName.ShouldBe(InstitutionNodeType.School.Name);
    }

    [Fact]
    public void Ayni_ilcedeki_iki_okul_tek_il_ve_tek_ilce_dugumu_paylasir()
    {
        var plan = InstitutionHierarchyPlanner.Plan(
            [Okul("Atatürk MTAL"), Okul("Cumhuriyet MTAL")], Ids());

        plan.Created.Count.ShouldBe(2);
    }

    [Fact]
    public void Ilcesiz_okul_dogrudan_il_altina_baglanir()
    {
        var okul = Okul("Merkez MTAL", ilce: null);

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        plan.Created.Count.ShouldBe(1);
        plan.Created.Single().NodeTypeName.ShouldBe(InstitutionNodeType.Province.Name);
        plan.Assignments.Single(a => a.Id == okul.Id).Path.Trim('/').Split('/').Length.ShouldBe(2);
    }

    /// <summary>
    /// İl kodu olmayan okul <b>köke bağlanmaz</b>. Bağlansaydı, herhangi bir il yetkilisinin
    /// alt ağacına düşen sahipsiz bir kayıt olurdu. Kapsamsız kalır ve sayılır — sayı
    /// boşluğu görünür kılar.
    /// </summary>
    [Fact]
    public void Il_kodu_olmayan_okul_kapsamsiz_kalir_ve_sayilir()
    {
        var okul = Okul("Künyesiz MTAL", il: null, ilce: null);

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        plan.Created.ShouldBeEmpty();
        plan.Assignments.ShouldBeEmpty();
        plan.SkippedNoProvince.ShouldBe([okul.Id]);
    }

    /// <summary>
    /// <b>İdempotanlık — bu geçişin tek kritik özelliği.</b> İlk planı uygulanmış gibi kabul
    /// edip ikinci kez planlarsak hiçbir düğüm ÜRETİLMEMELİ ve atamalar birebir aynı olmalı.
    /// </summary>
    [Fact]
    public void Ikinci_kosu_dugum_cogaltmaz_ve_ayni_agaci_uretir()
    {
        var okul = Okul("Atatürk MTAL");
        var ilkPlan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        // İlk planı diske yazılmış gibi uygula.
        var uygulanmis = new List<InstitutionRecord> { okul };

        foreach (var yeni in ilkPlan.Created)
        {
            uygulanmis.Add(new InstitutionRecord
            {
                Id = yeni.Id,
                InstitutionCode = 0,
                FullName = yeni.FullName,
                ParentId = yeni.ParentId,
                NodeTypeName = yeni.NodeTypeName,
                Path = yeni.Path,
                ProvinceCode = yeni.ProvinceCode,
                DistrictName = yeni.DistrictName
            });
        }

        foreach (var atama in ilkPlan.Assignments)
        {
            var kayit = uygulanmis.Single(i => i.Id == atama.Id);
            kayit.ParentId = atama.ParentId;
            kayit.NodeTypeName = atama.NodeTypeName;
            kayit.Path = atama.Path;
        }

        var ikinciPlan = InstitutionHierarchyPlanner.Plan(uygulanmis, Ids());

        ikinciPlan.Created.ShouldBeEmpty("İkinci koşu düğüm çoğaltmamalı — geçiş idempotenttir.");
        ikinciPlan.Assignments.OrderBy(a => a.Id).ShouldBe(
            ilkPlan.Assignments.OrderBy(a => a.Id));
    }

    /// <summary>
    /// Bozulmuş bir yol ikinci koşuda ONARILIR. Atamalar yalnız "eksik" satırlara değil,
    /// bütün düğümlere yazılır; aksi hâlde elle bozulmuş tek bir satır kalıcı olurdu.
    /// </summary>
    [Fact]
    public void Bozulmus_yol_yeniden_kosuda_onarilir()
    {
        var okul = Okul("Atatürk MTAL");
        okul.NodeTypeName = InstitutionNodeType.School.Name;
        okul.Path = "/bozuk/";

        var plan = InstitutionHierarchyPlanner.Plan([okul], Ids());

        plan.Assignments.Single(a => a.Id == okul.Id).Path.ShouldNotBe("/bozuk/");
    }
}
