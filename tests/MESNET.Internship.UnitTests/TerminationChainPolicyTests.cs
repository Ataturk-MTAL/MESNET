using MESNET.Internship.Core.Policies;
using MESNET.Internship.Core.ValueObjects;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Fesih onay zinciri: <b>koordinatör öğretmen → müdür yardımcısı → müdür</b>, sırayla (#218).
///
/// <para><b>Düzeltilen model hatası:</b> zincir daha önce beş onaycı sayıyordu — veli ve
/// işletme yetkilisi de "onaycı"ydı (<c>business-rules.md</c> §4.3 de öyle yazıyordu). Gerçek
/// kural farklı: <b>veli ve işletme TALEP EDER, onaylamaz.</b></para>
///
/// <list type="bullet">
///   <item>İşletme ya da veli fesih isterse: öğretmen → müdür yrd. → müdür; <b>müdür onayında
///   fesih tamamlanır.</b></item>
///   <item>Okul tek taraflı fesih edecekse: koordinatör öğretmen talep eder, müdür yrd. ve
///   müdür onaylar.</item>
/// </list>
///
/// <para>Her iki durumda da <b>onaycı üçlüsü aynıdır</b>; değişen yalnız talebi kimin açtığıdır
/// ve o bilgi <c>RequestedBy</c>/<c>ReasonType</c> ile zaten kaydediliyor.</para>
///
/// <para><b>Sıra zorunludur.</b> Müdür yardımcısı, öğretmen onaylamadan onaylayamaz. Eski model
/// sırayı dayatmıyordu; "sırayla" kuralı kodda karşılıksızdı.</para>
///
/// <para><b>Yan etkisi (#218'in asıl konusu):</b> işletme onayı zincirden çıkınca okulda staj
/// yapan (işverensiz, #159) öğrencinin zinciri de tamamlanabilir hâle gelir. Önceden
/// <c>IsComplete</c> işletme onayını koşulsuz aradığı için o zincir hiç kapanmıyordu ve tek
/// çıkış override'dı.</para>
/// </summary>
public sealed class TerminationChainPolicyTests
{
    private static TerminationApprovalChain Empty() => new();

    // ─── Sıra ────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Bos_zincirde_once_ogretmen_beklenir()
    {
        TerminationChainPolicy.NextStep(Empty()).ShouldBe(TerminationStep.Teacher);
    }

    [Fact]
    public void Ogretmen_onaylayinca_mudur_yardimcisi_beklenir()
    {
        var chain = Empty() with { TeacherApproved = true };

        TerminationChainPolicy.NextStep(chain).ShouldBe(TerminationStep.Deputy);
    }

    [Fact]
    public void Mudur_yardimcisi_onaylayinca_mudur_beklenir()
    {
        var chain = Empty() with { TeacherApproved = true, DeputyApproved = true };

        TerminationChainPolicy.NextStep(chain).ShouldBe(TerminationStep.Director);
    }

    /// <summary>Müdür onayında fesih tamamlanır — beklenen adım kalmaz.</summary>
    [Fact]
    public void Mudur_onayinda_zincir_tamamlanir()
    {
        var chain = Empty() with
        {
            TeacherApproved = true, DeputyApproved = true, DirectorApproved = true
        };

        TerminationChainPolicy.NextStep(chain).ShouldBeNull();
        chain.IsComplete().ShouldBeTrue();
    }

    [Fact]
    public void Baslamamis_zincirde_beklenen_adim_yoktur()
    {
        TerminationChainPolicy.NextStep(null).ShouldBeNull();
    }

    [Fact]
    public void Override_edilmis_zincirde_beklenen_adim_kalmaz()
    {
        TerminationChainPolicy.NextStep(Empty() with { IsOverridden = true }).ShouldBeNull();
    }

    // ─── Sıra dayatması ──────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Asıl kural (#218).</b> Sıra atlanamaz: müdür yardımcısı, öğretmen onaylamadan
    /// onaylayamaz. Eski model her onayı bağımsız bayrak olarak yazıyordu ve "sırayla"
    /// kuralının kodda karşılığı yoktu.
    /// </summary>
    [Theory]
    [InlineData("Deputy")]
    [InlineData("Director")]
    public void Sira_atlanamaz(string stepName)
    {
        var step = TerminationStep.FromName(stepName);

        TerminationChainPolicy.CanApprove(Empty(), step).ShouldBeFalse();
    }

    [Fact]
    public void Sirasi_gelen_adim_onaylanabilir()
    {
        TerminationChainPolicy.CanApprove(Empty(), TerminationStep.Teacher).ShouldBeTrue();

        var withTeacher = Empty() with { TeacherApproved = true };
        TerminationChainPolicy.CanApprove(withTeacher, TerminationStep.Deputy).ShouldBeTrue();
    }

    /// <summary>Aynı adım iki kez onaylanamaz — sırası geçmiştir.</summary>
    [Fact]
    public void Onaylanmis_adim_tekrar_onaylanamaz()
    {
        var chain = Empty() with { TeacherApproved = true };

        TerminationChainPolicy.CanApprove(chain, TerminationStep.Teacher).ShouldBeFalse();
    }

    [Fact]
    public void Override_sonrasi_hicbir_adim_onaylanamaz()
    {
        var chain = Empty() with { IsOverridden = true };

        TerminationChainPolicy.CanApprove(chain, TerminationStep.Teacher).ShouldBeFalse();
    }

    // ─── Adım tanımları ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Zincirde <b>yalnız üç adım</b> vardır. Veli ve işletme adımlarının kalması, olmayan bir
    /// yetkiyi varmış gibi gösterirdi.
    /// </summary>
    [Fact]
    public void Zincirde_yalniz_uc_adim_vardir()
    {
        // SmartEnum.List alfabetiktir; anlamlı olan tanım sırasıdır (Value).
        TerminationStep.List.OrderBy(s => s.Value).Select(s => s.Name)
            .ShouldBe(["Teacher", "Deputy", "Director"]);
    }

    [Fact]
    public void Adimlarin_turkce_karsiligi_ve_izni_vardir()
    {
        TerminationStep.Teacher.Slug.ShouldBe("Koordinatör Öğretmen");
        TerminationStep.Teacher.Permission.ShouldBe("internship:approve");

        TerminationStep.Deputy.Slug.ShouldBe("Müdür Yardımcısı");
        TerminationStep.Deputy.Permission.ShouldBe("internship:approve");

        TerminationStep.Director.Slug.ShouldBe("Müdür");
        TerminationStep.Director.Permission.ShouldBe("internship:manage");
    }

    // ─── İşverensiz staj (#159 etkileşimi) ───────────────────────────────────────────

    /// <summary>
    /// <b>#218'in asıl konusu.</b> Okulda staj yapan öğrencinin işletmesi yoktur; işletme onayı
    /// zincirden çıkınca o zincir de normal yoldan kapanabilir hâle gelir. Önceden tek çıkış
    /// override'dı ve her okulda staj fesihinde override kaydı doğardı.
    /// </summary>
    [Fact]
    public void Isverensiz_stajda_zincir_normal_yoldan_tamamlanir()
    {
        var chain = Empty() with
        {
            TeacherApproved = true, DeputyApproved = true, DirectorApproved = true
        };

        chain.IsComplete().ShouldBeTrue("İşletme onayı artık zincirde değil.");
        chain.IsOverridden.ShouldBeFalse("Override'a gerek kalmamalı.");
    }
}
