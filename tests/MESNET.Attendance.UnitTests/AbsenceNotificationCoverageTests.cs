using System.Reflection;
using MESNET.Attendance.Application.Handlers;
using MESNET.Attendance.Core.Policies;
using MESNET.Attendance.Shared.Events;
using Shouldly;
using Wolverine;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Kademeli bildirim <b>yapısal</b> kilidi (#247) — hangi olaylar eşiği ölçtürür, defter aynı
/// transaction'da yazılır mı, hüküm yolundan ayrı mı.
///
/// <para>Yüklemin kendisi <see cref="AbsenceNotificationPolicyTests"/> ile kilitli; buradaki
/// testler <b>bağlantıyı</b> kilitler.</para>
/// </summary>
public sealed class AbsenceNotificationCoverageTests
{
    private static readonly Type Handler = typeof(CheckAbsenceNotificationHandler);

    /// <summary>
    /// Sayılabilir küme her değiştiğinde eşik yeniden ölçülmeli — kayıt girildiğinde,
    /// <b>onaylandığında</b> ve düzeltildiğinde. Yalnız girişi dinlemek, onay bekleyen kaydın
    /// dışlanmasıyla (#252) birleşince bildirimi de sessizce öldürür.
    /// </summary>
    [Theory]
    [InlineData(typeof(AttendanceMarked))]
    [InlineData(typeof(AttendanceApproved))]
    [InlineData(typeof(AttendanceCorrected))]
    public void Sayaci_degistiren_olay_esigi_olcturur(Type olayTipi)
    {
        HandlerFor(olayTipi).ShouldNotBeNull($"{olayTipi.Name} için eşik ölçümü yok.");
    }

    /// <summary>
    /// Dönüş <c>OutgoingMessages</c> olmalı: iki ayak (mazeretsiz + toplam) aynı çağrıda birden
    /// bildirim üretebilir ve tekil dönüş ikincisini sessizce yutardı.
    /// </summary>
    [Theory]
    [InlineData(typeof(AttendanceMarked))]
    [InlineData(typeof(AttendanceApproved))]
    [InlineData(typeof(AttendanceCorrected))]
    public void Donus_birden_cok_bildirim_tasiyabilir(Type olayTipi)
    {
        HandlerFor(olayTipi)!.ReturnType.ShouldBe(typeof(Task<OutgoingMessages>),
            "İki ayak aynı anda dolabilir; tekil dönüş ikinci bildirimi yutar.");
    }

    /// <summary>
    /// Defter <b>yazma</b> oturumuyla aynı transaction'da güncellenmeli. <c>IQuerySession</c>
    /// alınsaydı kademe ilerlemesi kalıcı olmaz ve aynı kademe her olayda yeniden bildirilirdi.
    /// </summary>
    [Theory]
    [InlineData(typeof(AttendanceMarked))]
    [InlineData(typeof(AttendanceApproved))]
    [InlineData(typeof(AttendanceCorrected))]
    public void Defter_yazma_oturumuyla_guncellenir(Type olayTipi)
    {
        var oturum = HandlerFor(olayTipi)!.GetParameters()[1].ParameterType;

        oturum.Name.ShouldBe("IDocumentSession",
            "Kademe ilerlemesi mesajla aynı transaction'da commit olmalı.");
    }

    /// <summary>
    /// <b>Tebligat ile hüküm ayrı yollardır.</b> Bildirim olayı fesih zincirini besleyen
    /// handler'a girmemeli — md. 36 (4) tebligatı, md. 36 (5) feshi ayrı fıkralardır.
    /// </summary>
    [Fact]
    public void Bildirim_olayi_fesih_zincirine_girmez()
    {
        typeof(CheckAttendanceLimitHandler)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name is "Handle" or "HandleAsync")
            .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType)
            .ShouldNotContain(typeof(AbsenceNotificationDue));
    }

    /// <summary>Kademeler mevzuatın lafzıdır — md. 36 (4): 5., 15., 25. gün.</summary>
    [Fact]
    public void Kademeler_mevzuatla_ayni()
    {
        AbsenceNotificationPolicy.Steps.ShouldBe([5, 15, 25]);
    }

    private static MethodInfo? HandlerFor(Type olayTipi) => Handler
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.Name is "Handle" or "HandleAsync")
        .FirstOrDefault(m => m.GetParameters().FirstOrDefault()?.ParameterType == olayTipi);
}
