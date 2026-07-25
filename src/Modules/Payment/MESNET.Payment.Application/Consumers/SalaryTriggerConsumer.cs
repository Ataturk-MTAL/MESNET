using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.Entities;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Devamsızlık kaydından maaş sürecini tetikler — ilgili öğrenci/ay için henüz ödeme kaydı
/// yoksa <see cref="CalculateMonthlySalary"/> yayınlar.
/// </summary>
/// <remarks>
/// Önceden <c>PaymentSaga.StartAsync</c> doğrudan <c>AttendanceMarked</c> ile başlıyor ve her
/// çağrıda <c>Guid.NewGuid()</c> ürettiği için aynı öğrenci/ay'a onlarca saga + ödeme kaydı
/// açıyordu (#62). Bu ara katman iki koruma getiriyor:
///
/// 1. Kimlik deterministik — <see cref="SalaryPeriodId"/> (öğrenci, ay) ikilisinden türetiliyor.
/// 2. Ödeme kaydı zaten varsa hiç tetiklenmiyor; böylece onay süreci ilerlemiş bir saga
///    (öğrenci onayladı, öğretmen onayladı) yeni bir devamsızlık girişiyle
///    <c>AwaitingReceipt</c>'e geri sarılmıyor.
///
/// Kalan dar yarış: aynı anda işlenen iki <c>AttendanceMarked</c> ikisi de kaydı yok görüp komutu
/// yayınlayabilir. Kimlik deterministik olduğu için sonuç yine tek satırdır ve iki hesap da aynı
/// değerleri ürettiğinden zararsızdır — tehlikeli olan "ilerlemiş saga'yı sıfırlama" durumu
/// yukarıdaki kontrolle kapalıdır.
/// </remarks>
public static class SalaryTriggerConsumer
{
    public static async Task<CalculateMonthlySalary?> Handle(
        AttendanceMarked @event, IQuerySession session)
    {
        var month = @event.Date.ToString("yyyy-MM");
        var salaryPeriodId = SalaryPeriodId.For(@event.StudentId, month);

        var existing = await session.LoadAsync<PaymentSummary>(salaryPeriodId);
        if (existing is not null) return null;

        return new CalculateMonthlySalary(
            salaryPeriodId,
            @event.StudentId,
            @event.BusinessId,
            @event.InstitutionId,
            @event.AcademicPeriodId,
            month,
            @event.Date);
    }
}
