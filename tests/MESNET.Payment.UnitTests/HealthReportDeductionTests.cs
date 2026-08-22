using System.Reflection;
using MESNET.Attendance.Shared.Events;
using MESNET.Payment.Application.Consumers;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Sağlık raporunun ücret kesintisini ne zaman kaldırdığı (#172).
///
/// <para><b>İki ayrı boşluk kapatıldı.</b> Birincisi: <c>HealthReportAttached</c> Payment'ta
/// HİÇ dinlenmiyordu; Attendance modülünde tür <c>HealthReport</c>'a dönse bile Payment'ın
/// yerel kaydı eski türde kalıyor ve geçerli raporu olan öğrencinin ücreti kesilmeye devam
/// ediyordu. İkincisi: rapor girişi işletme yetkilisinde de bulunduğu için, yüklemeyi koşulsuz
/// dinlemek ödemeyi yapan tarafın kendi kesintisini tek taraflı kaldırması demek olurdu.</para>
///
/// <para>Kural: kesinti yalnız (a) koordinatör öğretmen onayında ya da (b) okul tarafının
/// doğrudan girdiği raporda kalkar.</para>
/// </summary>
public sealed class HealthReportDeductionTests
{
    [Fact]
    public void Onaylanan_rapor_tuketilir()
    {
        HasConsumerFor(typeof(AbsenceTallyConsumer), typeof(HealthReportApproved))
            .ShouldBeTrue(
                "Onay para etkisini doğuran adımdır; tüketilmezse onaylanan rapora rağmen "
                + "kesinti uygulanmaya devam eder (#172).");
    }

    [Fact]
    public void Rapor_yuklemesi_tuketilir()
    {
        HasConsumerFor(typeof(AbsenceTallyConsumer), typeof(HealthReportAttached))
            .ShouldBeTrue(
                "Okul tarafının doğrudan girdiği rapor onay beklemez; yükleme tüketilmezse "
                + "o kayıtta kesinti hiç kalkmaz (#172).");
    }

    /// <summary>
    /// Olayın onay gerekip gerekmediğini <b>taşıması</b> zorunludur: Payment kararı bu alandan
    /// verir. Alan kaldırılırsa yükleme koşulsuz kesinti kaldırır ve işletme kendi kesintisini
    /// iptal edebilir hâle gelir.
    /// </summary>
    [Fact]
    public void Yukleme_olayi_onay_gerekliligini_tasir()
    {
        typeof(HealthReportAttached)
            .GetProperty(nameof(HealthReportAttached.RequiresApproval))
            .ShouldNotBeNull("Payment'ın kesinti kararı bu alana bağlıdır (#172).");
    }

    /// <summary>
    /// Ücret kesintisine tabi türler <c>Unexcused</c> ve <c>UnpaidLeave</c>'dir
    /// (business-rules.md §6.2). <c>HealthReport</c> bu kümede OLMAMALIDIR — onay zincirinin
    /// sonunda kesintinin kalkmasının nedeni budur.
    /// </summary>
    [Fact]
    public void Saglik_raporu_kesintiye_tabi_turler_arasinda_degildir()
    {
        var deductibleTypes = typeof(MESNET.Payment.Application.Sagas.PaymentSaga)
            .GetField("DeductibleAbsenceTypes", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null) as string[];

        deductibleTypes.ShouldNotBeNull();
        deductibleTypes.ShouldNotContain("HealthReport");
        deductibleTypes.ShouldContain("Unexcused");
    }

    private static bool HasConsumerFor(Type consumerType, Type eventType) =>
        consumerType
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.Name is "Consume" or "ConsumeAsync" or "Handle" or "HandleAsync")
            .Select(m => m.GetParameters().FirstOrDefault())
            .Any(p => p?.ParameterType == eventType);
}
