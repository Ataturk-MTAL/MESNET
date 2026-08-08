using MESNET.Internship.Core.Policies;
using MESNET.Internship.Core.ValueObjects;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Fesih onay zincirinde <b>hangi adımların beklediğini</b> hesaplar (#191).
///
/// <para><b>Neden gerekli:</b> zincirin durumu bugün hiçbir uçtan okunamıyor —
/// <c>TerminationApprovalChainDto</c> ve mapper'ı var ama hiçbir handler onları çağırmıyor.
/// Arayüzde "hangi adımda takıldı" gösterilebilmesi için önce bu kararın adı konmalı.</para>
///
/// <para><b>Zincir SIRALI DEĞİLDİR.</b> Saga her onayı bağımsız bir bayrak olarak yazar;
/// müdür, öğretmenden önce onaylayabilir. Bu yüzden politika "sıradaki adım" değil
/// <b>bekleyen adımların kümesini</b> döndürür. Arayüz bunlardan ilkini vurgulayabilir, ama
/// kod hiçbir yerde sıra dayatmamalı — dayatsaydı gerçekte olabilen bir durumu "imkânsız"
/// sayardı.</para>
/// </summary>
public sealed class TerminationChainPolicyTests
{
    private static TerminationApprovalChain Bos() => new();

    private static TerminationApprovalChain Tam() => new()
    {
        ParentApproved = true,
        TeacherApproved = true,
        DeputyApproved = true,
        DirectorApproved = true,
        BusinessRepApproved = true
    };

    [Fact]
    public void Bos_zincirde_tum_adimlar_bekler()
    {
        var bekleyen = TerminationChainPolicy.PendingSteps(Bos(), requiresParent: true);

        bekleyen.ShouldBe([
            TerminationStep.Parent,
            TerminationStep.Teacher,
            TerminationStep.Deputy,
            TerminationStep.Director,
            TerminationStep.BusinessRep
        ]);
    }

    /// <summary>
    /// Veli adımı 18 yaş üstü öğrencide istenmez — saga <c>RequiresParentApproval</c> ile
    /// karar verir, politika o kararı tekrar üretmez, <b>uygular</b>.
    /// </summary>
    [Fact]
    public void Veli_gerekmiyorsa_o_adim_beklemez()
    {
        TerminationChainPolicy.PendingSteps(Bos(), requiresParent: false)
            .ShouldNotContain(TerminationStep.Parent);
    }

    [Fact]
    public void Onaylanan_adim_bekleyenlerden_cikar()
    {
        var zincir = Bos() with { TeacherApproved = true, DirectorApproved = true };

        TerminationChainPolicy.PendingSteps(zincir, requiresParent: true)
            .ShouldBe([TerminationStep.Parent, TerminationStep.Deputy, TerminationStep.BusinessRep]);
    }

    /// <summary>
    /// <b>Sıra dayatılmaz.</b> Müdür, öğretmenden önce onaylayabilir; bu geçerli bir durumdur
    /// ve politika onu "atlanmış adım" saymaz.
    /// </summary>
    [Fact]
    public void Sira_disi_onay_gecerlidir()
    {
        var zincir = Bos() with { DirectorApproved = true };

        TerminationChainPolicy.PendingSteps(zincir, requiresParent: true)
            .ShouldNotContain(TerminationStep.Director);
    }

    [Fact]
    public void Tamamlanmis_zincirde_bekleyen_adim_kalmaz()
    {
        TerminationChainPolicy.PendingSteps(Tam(), requiresParent: true).ShouldBeEmpty();
    }

    /// <summary>
    /// Override zinciri <b>tümüyle</b> kapatır — eksik adımlar "bekliyor" diye gösterilmemeli,
    /// yoksa arayüz kapanmış bir süreci hâlâ açık sanır.
    /// </summary>
    [Fact]
    public void Override_edilmis_zincirde_bekleyen_adim_kalmaz()
    {
        var zincir = Bos() with { IsOverridden = true };

        TerminationChainPolicy.PendingSteps(zincir, requiresParent: true).ShouldBeEmpty();
    }

    /// <summary>
    /// Zincir hiç başlamamışsa (<c>null</c>) bekleyen adım yoktur — fesih süreci açılmamıştır.
    /// "Hepsi bekliyor" demek, olmayan bir süreci varmış gibi gösterirdi.
    /// </summary>
    [Fact]
    public void Baslamamis_zincirde_bekleyen_adim_yoktur()
    {
        TerminationChainPolicy.PendingSteps(null, requiresParent: true).ShouldBeEmpty();
    }

    [Fact]
    public void Adimlarin_turkce_karsiligi_vardir()
    {
        TerminationStep.Parent.Slug.ShouldBe("Veli");
        TerminationStep.Teacher.Slug.ShouldBe("Koordinatör Öğretmen");
        TerminationStep.Deputy.Slug.ShouldBe("Müdür Yardımcısı");
        TerminationStep.Director.Slug.ShouldBe("Müdür");
        TerminationStep.BusinessRep.Slug.ShouldBe("İşletme Yetkilisi");
    }

    /// <summary>
    /// Her adımın onay ucu ve izni <b>tek yerde</b> tanımlı olmalı; arayüz butonu hangi
    /// kullanıcıya göstereceğine buradan karar verecek. İki yere yazılsaydı biri değişip
    /// diğeri unutulduğunda buton yanlış kişiye görünürdü.
    /// </summary>
    [Fact]
    public void Her_adim_kendi_ucunu_ve_iznini_tasir()
    {
        TerminationStep.Parent.Endpoint.ShouldBe("parent");
        TerminationStep.Parent.Permission.ShouldBe("internship:approve:parent");

        TerminationStep.Teacher.Permission.ShouldBe("internship:approve");
        TerminationStep.Deputy.Permission.ShouldBe("internship:approve");
        TerminationStep.Director.Permission.ShouldBe("internship:manage");
        TerminationStep.BusinessRep.Permission.ShouldBe("company:student:manage");
    }
}
