using System.Reflection;
using MESNET.Contract.Shared.Events;
using MESNET.Payment.Application.Consumers;
using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Maaş dönemi kimliğinin (sözleşme, ay) ikilisine taşınması ve ay içi devir senaryosu (#154).
///
/// <para><b>Hata neydi:</b> anahtar (öğrenci, ay) idi, yani bir öğrenci için ayda TEK dönem
/// açılabiliyordu. Ay ortasında işletme değiştiğinde iki işverenin ayrı yükümlülüğü tek kayda
/// sıkışıyordu. #152'den sonra belirti şu hâle gelmişti: ayrılınan işletme için dönem HİÇ
/// açılmıyor (kapalı yerleştirme ay sonu koşusundan düşüyor), öğrenci orada çalıştığı günlerin
/// ücretini alamıyordu.</para>
/// </summary>
public sealed class ContractSalaryPeriodTests
{
    private const string Month = "2026-07";

    // ── Kimlik ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void Ayni_ogrencinin_iki_sozlesmesi_ayni_ayda_FARKLI_donem_uretir()
    {
        // #154'ün çekirdeği. Eski anahtarla bu iki çağrı aynı Guid'i üretiyor, ikinci sözleşme
        // "zaten var" diye atlanıyordu.
        var ayrilanSozlesme = Guid.NewGuid();
        var yeniSozlesme = Guid.NewGuid();

        SalaryPeriodId.For(ayrilanSozlesme, Month)
            .ShouldNotBe(SalaryPeriodId.For(yeniSozlesme, Month));
    }

    [Fact]
    public void Ayni_sozlesme_ve_ay_her_zaman_ayni_kimligi_uretir()
    {
        // Deterministik kimlik #62'nin çözümüydü: tekrar tetikleme ikinci kayıt açmamalı.
        var contractId = Guid.NewGuid();

        SalaryPeriodId.For(contractId, Month)
            .ShouldBe(SalaryPeriodId.For(contractId, Month));
    }

    [Fact]
    public void Ayni_sozlesmenin_farkli_aylari_farkli_donem_uretir()
    {
        var contractId = Guid.NewGuid();

        SalaryPeriodId.For(contractId, "2026-07")
            .ShouldNotBe(SalaryPeriodId.For(contractId, "2026-08"));
    }

    // ── Ay içi devir: uçtan uca sayı ───────────────────────────────────────────────

    [Fact]
    public void Ay_ici_fesih_ve_yeni_sozlesme_iki_ayri_donem_ve_boluşulmuş_ucret_uretir()
    {
        // Senaryo: öğrenci 15 Temmuz'da A işletmesinden ayrılıyor, 16 Temmuz'da B'de başlıyor.
        // Beklenen: iki ayrı dönem, her biri kendi istihdam günü kadar ücret ve teşvik.
        var sozlesmeA = Guid.NewGuid();
        var sozlesmeB = Guid.NewGuid();

        var gunA = EmploymentDays.InMonth(
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc), 2026, 7);
        var gunB = EmploymentDays.InMonth(
            new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), null, 2026, 7);

        gunA.ShouldBe(15);
        gunB.ShouldBe(16);

        SalaryPeriodId.For(sozlesmeA, Month).ShouldNotBe(SalaryPeriodId.For(sozlesmeB, Month));

        var config = new SalaryCalculationConfig
        {
            MinimumWage = 20_000m,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var a = Hesapla(config, gunA);
        var b = Hesapla(config, gunB);

        // Taban 20.000 × 0,30 = 6.000 → günlük 200
        a.NetAmount.ShouldBe(3_000m, tolerance: 0.01m);
        b.NetAmount.ShouldBe(3_200m, tolerance: 0.01m);

        // Teşvik de aynı oranda: tam ay 2.000 → 15 günde 1.000, 16 günde 1.066,67
        a.GovernmentContribution.ShouldBe(1_000m, tolerance: 0.01m);
        b.GovernmentContribution.ShouldBe(1_066.67m, tolerance: 0.01m);

        // Eski davranışta A hiç hesaplanmıyor, B tam ay (6.000) alıyordu.
        a.NetAmount.ShouldBeGreaterThan(0m);
        b.NetAmount.ShouldBeLessThan(6_000m);
    }

    private static SalaryCalculator.Result Hesapla(SalaryCalculationConfig config, int employedDays)
        => SalaryCalculator.Calculate(
            config,
            personnelCount: 25,
            educationTypeName: "Anadolu",
            classYear: 11,
            hasJourneymanQualification: false,
            deductibleAbsenceDays: 0,
            employedDays: employedDays);

    // ── Sözleşme kaydının olay kapsaması ───────────────────────────────────────────

    /// <summary>
    /// Sözleşmenin istihdam penceresini kuran/kapatan olaylar. Biri tüketilmezse pencere
    /// yanlış olur ve para yanlış hesaplanır: <c>ContractCreated</c> yoksa dönem hiç açılmaz,
    /// <c>ContractTerminated</c> yoksa biten sözleşmeye ay sonuna kadar ücret yazılır.
    /// </summary>
    public static TheoryData<Type> EmploymentWindowEvents =>
    [
        typeof(ContractCreated),      // pencerenin alt ucu + ücret taahhüdü + kurum/dönem
        typeof(ContractActivated),    // istihdamın fiilen başlaması (taslak sözleşmeye maaş açılmaz)
        typeof(ContractTerminated),   // üst uç — fesih
        typeof(ContractCompleted),    // üst uç — başarıyla tamamlama
    ];

    [Theory]
    [MemberData(nameof(EmploymentWindowEvents))]
    public void Istihdam_penceresini_belirleyen_her_olayin_tuketicisi_vardir(Type eventType)
    {
        HasConsumerFor(typeof(ContractEmploymentConsumer), eventType)
            .ShouldBeTrue(
                $"{eventType.Name} sözleşmenin istihdam penceresini belirler; "
                + "ContractEmploymentConsumer onu tüketmek ZORUNDADIR (#154). Aksi hâlde gün "
                + "oranlaması yanlış tabana oturur ve maaş yanlış hesaplanır.");
    }

    private static bool HasConsumerFor(Type consumerType, Type eventType) =>
        consumerType
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.Name is "Consume" or "ConsumeAsync" or "Handle" or "HandleAsync")
            .Select(m => m.GetParameters().FirstOrDefault())
            .Any(p => p?.ParameterType == eventType);
}
