using System.Reflection;
using MESNET.Attendance.Shared.Events;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Consumers;
using MESNET.Payment.Application.Sagas;
using MESNET.Payment.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Devamsızlık kaydı değişince maaş yeniden hesaplanır (#255) — <b>yapısal kilit</b>.
///
/// <para><b>Bulunan açık:</b> <c>RecalculateMonthlySalary</c>'nin tek üreticisi
/// <c>SalaryTriggerConsumer</c>'dı ve yalnız <c>AttendanceMarked</c> dinliyordu. Devamsızlık
/// onaylandığında (<c>Pending → Recorded</c>, kaydı kesinti kümesine <b>sokan</b> adım) ya da
/// düzeltildiğinde hiçbir yeniden hesap tetiklenmiyordu. Ay sonu koşusundan sonra gelen onay,
/// kesintiyi <b>kalıcı olarak</b> kaybettiriyordu: <c>SalaryTriggerConsumer</c> ayı
/// <c>@event.Date</c>'ten türetiyor ve "yalnız bu hafta" kısıtı yüzünden o aya yeni bir
/// <c>AttendanceMarked</c> girilemiyordu.</para>
///
/// <para><b>Tetik neden ayrı tüketiciye konmadı:</b> <c>MultipleHandlerBehavior.Separated</c>
/// altında aynı mesajı işleyen iki tüketici ayrı sticky local queue'da, ayrı transaction'da ve
/// <b>sırasız</b> koşar. Tetikleyici önce koşarsa saga bayat <c>StudentAbsenceView</c> okur,
/// kesintiyi eski değerle hesaplar ve "tutar değişmediyse sus" kısayolu yüzünden dönem bir daha
/// hiç tetiklenmez. Cascading dönüş bunu yapısal olarak imkânsız kılar: komut, görünümün
/// yazıldığı aynı Marten transaction'ında outbox'a girer ve yalnız commit sonrası salınır.</para>
///
/// <para><b>Sınır — dürüstçe:</b> bu testler <b>yapıyı</b> kilitler (hangi olay tetikler, dönüş
/// tipi nedir, karar tek yerde mi). "Sayılabilirlik değişti mi" mantığı <c>IDocumentSession</c>
/// gerektirdiği için burada koşturulamaz; yüklem
/// <see cref="AbsenceDeductionPolicyTests"/> ile ayrıca kilitli.</para>
/// </summary>
public sealed class SalaryRecalculationTriggerTests
{
    private static readonly Type Tally = typeof(AbsenceTallyConsumer);

    /// <summary>
    /// Kesinti kümesini değiştirebilen her olay yeniden hesap <b>döndürebilmeli</b>.
    /// </summary>
    [Theory]
    [InlineData(typeof(AttendanceMarked))]
    [InlineData(typeof(AttendanceApproved))]
    [InlineData(typeof(AttendanceCorrected))]
    [InlineData(typeof(AttendanceDeleted))]
    [InlineData(typeof(HealthReportApproved))]
    [InlineData(typeof(HealthReportAttached))]
    public void Kesintiyi_degistiren_olay_yeniden_hesap_dondurur(Type olayTipi)
    {
        var metot = HandlerFor(olayTipi);

        metot.ShouldNotBeNull($"{olayTipi.Name} için tüketici yok.");
        metot.ReturnType.ShouldBe(typeof(Task<RecalculateMonthlySalary?>),
            $"{olayTipi.Name} kesinti kümesini değiştirebilir; yeniden hesap döndürmezse "
            + "tutar sessizce eski kalır.");
    }

    /// <summary>
    /// <c>AttendanceVerified</c> tutarı etkilemez (<c>Recorded</c>/<c>Corrected</c> zaten
    /// sayılıyordu) ama tüketicisi kalmalı — durum ekseni Payment'ta güncel tutulur.
    /// </summary>
    [Fact]
    public void Dogrulama_olayi_hala_tuketiliyor()
    {
        HandlerFor(typeof(AttendanceVerified)).ShouldNotBeNull();
    }

    /// <summary>
    /// Eski ayrı tetikleyici <b>kaldırıldı</b>. Geri gelirse yarış da geri gelir: aynı mesajı
    /// işleyen ikinci bir tüketici bayat görünüm okur.
    /// </summary>
    [Fact]
    public void Ayri_tetikleyici_tuketici_geri_gelmemeli()
    {
        Tally.Assembly.GetTypes()
            .Any(t => t.Name == "SalaryTriggerConsumer")
            .ShouldBeFalse(
                "Tetikleyici, sayacı güncelleyen tüketicinin DÖNÜŞÜ olmalı; ayrı tüketici "
                + "sıra garantisi olmadığı için bayat görünüm okur ve kesinti sessizce kaybolur.");
    }

    /// <summary>
    /// Sayım kararı <b>tek yerde</b>. <c>PaymentSaga</c> kendi kopyasını tutarsa tetikleyiciyle
    /// sessizce ayrışır: tetik "değişmedi" der, hesap hiç koşmaz.
    /// </summary>
    [Fact]
    public void Kesinti_turleri_tek_kaynaktan_gelir()
    {
        var sagaAlani = typeof(PaymentSaga)
            .GetField("DeductibleAbsenceTypes", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null) as string[];

        sagaAlani.ShouldBeSameAs(AbsenceDeductionPolicy.DeductibleAbsenceTypes,
            "PaymentSaga kendi kopyasını tutmamalı — AbsenceDeductionPolicy tek kaynaktır.");
    }

    /// <summary>
    /// Fan-out gerçektir: ücretli izin onayı aralıktaki <b>her gün</b> için ayrı olay yayınlar,
    /// hepsi aynı döneme gider. Eşzamanlılık politikası yoksa kaybeden çağrı dead letter'a düşer
    /// ve yeniden hesap kalıcı olarak kaybolur.
    /// </summary>
    [Fact]
    public void Saga_esszamanlilik_politikasi_tanimli()
    {
        typeof(PaymentSaga)
            .GetMethod("Configure", BindingFlags.Public | BindingFlags.Static)
            .ShouldNotBeNull("PaymentSaga.Configure ile SagaConcurrencyException yeniden denenmeli.");
    }

    /// <summary>
    /// Silinmiş saga'ya gelen gecikmiş komut sessizce düşmeli — <c>UnknownSagaException</c> ile
    /// dead letter'a gitmemeli.
    /// </summary>
    [Fact]
    public void Saga_bulunamadiginda_sessizce_dusulur()
    {
        var notFound = typeof(PaymentSaga)
            .GetMethod("NotFound", BindingFlags.Public | BindingFlags.Static);

        notFound.ShouldNotBeNull();
        notFound.GetParameters().Single().ParameterType.ShouldBe(typeof(RecalculateMonthlySalary));
    }

    private static MethodInfo? HandlerFor(Type olayTipi) => Tally
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.Name is "Consume" or "ConsumeAsync" or "Handle" or "HandleAsync")
        .FirstOrDefault(m => m.GetParameters().FirstOrDefault()?.ParameterType == olayTipi);
}
