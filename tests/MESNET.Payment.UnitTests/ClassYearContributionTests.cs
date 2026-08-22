using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using MESNET.Payment.Core.ReadModels;
using MESNET.Payment.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Sınıf tekrarında devlet katkısının kesilmesi (#161).
///
/// <para><b>Kural:</b> bir öğrenci belirli bir <b>sınıf yılı</b> için devlet katkısını bir kez
/// alır. O sınıf yılı tekrar edildiğinde katkı hesaplanmaz. Katkı alınmamış bir sınıf yılına
/// terfi edildiğinde katkı yeniden işler.</para>
///
/// <para><b>Ücret etkilenmez.</b> Katkı işletmeye ödenir; öğrenci parasını işletmeden alır.
/// Bloke, öğrencinin aldığı ücreti değil <b>işveren payını</b> (Net − Katkı) yükseltir.
/// MESEM'de katkı en az ücretin tamamı olduğu için işletmenin maliyeti sıfırdan tam ücrete
/// çıkar — kuralın en sezgiye aykırı yanı budur.</para>
///
/// <para><b>Şartname düzeltmesi:</b> "kayıt varsa katkı yok" kuralı tek başına yanlıştır —
/// katkı aylık, sınıf yılı 9–10 aylıktır. Kayıt hangi akademik dönemde açıldığını da tutar;
/// bloke yalnız <b>farklı</b> akademik dönemde aynı sınıf yılı görülünce devreye girer.</para>
/// </summary>
public sealed class ClassYearContributionTests
{
    private const decimal MinimumWage = 20_000m;

    private static SalaryCalculationConfig Config() => new()
    {
        MinimumWage = MinimumWage,
        EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static SalaryCalculator.Result Calculate(
        bool isClassYearContributionExhausted = false,
        string educationType = "Anadolu",
        bool isPublicInstitution = false)
        => SalaryCalculator.Calculate(
            Config(),
            personnelCount: 25,
            educationTypeName: educationType,
            classYear: 12,
            hasJourneymanQualification: true,
            deductibleAbsenceDays: 0,
            isPublicInstitution: isPublicInstitution,
            isClassYearContributionExhausted: isClassYearContributionExhausted);

    // ── Hesap ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Tekrar_edilen_sinif_yilinda_katki_sifirdir()
    {
        var result = Calculate(isClassYearContributionExhausted: true);

        result.GovernmentContribution.ShouldBe(0m);
        result.ContributionType.ShouldBe(GovernmentContributionType.ClassYearRepeated);
    }

    [Fact]
    public void Tekrar_edilen_sinif_yilinda_UCRET_degismez()
    {
        // Kuralın çekirdeği: kalkan yalnız katkı. Öğrenci çalışıyor, ücretini alıyor.
        var bloke = Calculate(isClassYearContributionExhausted: true);
        var normal = Calculate();

        bloke.BaseWage.ShouldBe(normal.BaseWage);
        bloke.NetAmount.ShouldBe(normal.NetAmount);
        bloke.Deduction.ShouldBe(normal.Deduction);
    }

    [Fact]
    public void Blokeli_ayda_isveren_payi_tam_ucrete_cikar()
    {
        // MESEM 12 + kalfalık: katkı normalde en az ücretin TAMAMI (10.000).
        // Bloke olunca işveren payı (Net − Katkı) net ücretin tamamına çıkar.
        var normal = Calculate(educationType: "Mesem");
        var bloke = Calculate(educationType: "Mesem", isClassYearContributionExhausted: true);

        (normal.NetAmount - normal.GovernmentContribution).ShouldBe(0m, tolerance: 0.01m);
        (bloke.NetAmount - bloke.GovernmentContribution).ShouldBe(bloke.NetAmount);
    }

    [Fact]
    public void Bloke_yokken_katki_normal_hesaplanir()
    {
        // Regresyon: #83/#84/#157'de kilitlenen davranış bozulmamalı.
        Calculate().GovernmentContribution.ShouldBe(2_000m, tolerance: 0.01m);
    }

    [Fact]
    public void Kamu_kurumu_kontrolu_sinif_tekrarindan_ONCE_gelir()
    {
        // İkisi de sıfır üretir; hangi gerekçenin kaydedileceği belirsiz kalmamalı.
        // Kamu kurumunda katkı zaten hiç doğmaz — sınıf yılı "tüketilmiş" sayılmamalı.
        var result = Calculate(isPublicInstitution: true, isClassYearContributionExhausted: true);

        result.GovernmentContribution.ShouldBe(0m);
        result.ContributionType.ShouldBe(GovernmentContributionType.PublicInstitution);
    }

    // ── Kayıt politikası (saf karar) ───────────────────────────────────────────────

    [Fact]
    public void Ayni_akademik_donemin_ikinci_ayinda_katki_bloke_OLMAZ()
    {
        // Şartname düzeltmesinin kilidi: katkı aylık, sınıf yılı 9–10 aylık. Bu kontrol
        // olmasaydı öğrenci hiç sınıfta kalmadan Kasım ayından itibaren katkısını kaybederdi.
        var donem = Guid.NewGuid();
        var kayit = new ClassYearContributionClaim
        {
            StudentId = Guid.NewGuid(), ClassYear = 11, FirstAcademicPeriodId = donem
        };

        ClassYearContributionPolicy.IsExhausted(kayit, currentAcademicPeriodId: donem)
            .ShouldBeFalse();
    }

    [Fact]
    public void Sonraki_akademik_donemde_ayni_sinif_yili_bloke_EDER()
    {
        // Aynı sınıf yılı + farklı akademik dönem = tanım gereği sınıf tekrarı.
        var kayit = new ClassYearContributionClaim
        {
            StudentId = Guid.NewGuid(), ClassYear = 11, FirstAcademicPeriodId = Guid.NewGuid()
        };

        ClassYearContributionPolicy.IsExhausted(kayit, currentAcademicPeriodId: Guid.NewGuid())
            .ShouldBeTrue();
    }

    [Fact]
    public void Kayit_yoksa_bloke_olmaz()
    {
        // Terfi hâli: 12. sınıfın kaydı yoktur, katkı yeniden işler.
        ClassYearContributionPolicy.IsExhausted(null, currentAcademicPeriodId: Guid.NewGuid())
            .ShouldBeFalse();
    }

    // ── Kimlik ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Kayit_kimligi_ogrenci_ve_sinif_yilindan_deterministik_uretilir()
    {
        var studentId = Guid.NewGuid();

        ContributionClaimId.For(studentId, 11)
            .ShouldBe(ContributionClaimId.For(studentId, 11));
    }

    [Fact]
    public void Farkli_sinif_yillari_farkli_kayit_uretir()
    {
        // Terfi edince yeni sınıf yılının kaydı ayrıdır; eskisi yeni yılı bloke etmez.
        var studentId = Guid.NewGuid();

        ContributionClaimId.For(studentId, 11)
            .ShouldNotBe(ContributionClaimId.For(studentId, 12));
    }

    [Fact]
    public void Farkli_ogrenciler_ayni_sinifta_farkli_kayit_uretir()
    {
        ContributionClaimId.For(Guid.NewGuid(), 11)
            .ShouldNotBe(ContributionClaimId.For(Guid.NewGuid(), 11));
    }

    // ── Kaydın yazıldığı yol ───────────────────────────────────────────────────────

    [Fact]
    public void Katki_kaydi_odeme_TAMAMLANDIGINDA_yazilir()
    {
        // Yazma anı kritik: hesap anında yazılsaydı sonradan reddedilen bir ödeme öğrenciyi
        // hiç almadığı bir katkı için bloke ederdi. Tüketici PaymentCompleted'ı dinlemek
        // ZORUNDA — bu bağ kopsa kural sessizce hiç çalışmaz (#152'nin sınıfı).
        typeof(MESNET.Payment.Application.Consumers.ContributionClaimConsumer)
            .GetMethods()
            .Where(m => m.Name is "Consume" or "ConsumeAsync" or "Handle" or "HandleAsync")
            .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
            .ShouldContain(typeof(MESNET.Payment.Shared.Events.PaymentCompleted));
    }

    [Fact]
    public void Tamamlanma_olayi_kaydi_yazmak_icin_gereken_ucu_de_tasir()
    {
        // Tüketici bu üçünü profilden okuyamaz: onay ayın sonunda gelir, öğrencinin profili
        // o an bir sonraki sınıfa geçmiş olabilir ve katkı yanlış sınıf yılına yazılırdı.
        var alanlar = typeof(MESNET.Payment.Shared.Events.PaymentCompleted)
            .GetProperties().Select(p => p.Name).ToList();

        alanlar.ShouldContain(nameof(MESNET.Payment.Shared.Events.PaymentCompleted.ClassYear));
        alanlar.ShouldContain(nameof(MESNET.Payment.Shared.Events.PaymentCompleted.AcademicPeriodId));
        alanlar.ShouldContain(nameof(MESNET.Payment.Shared.Events.PaymentCompleted.GovernmentContribution));
    }
}
