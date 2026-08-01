using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Devamsızlık kaydından maaş sürecini tetikler — devamsızlığın düştüğü sözleşmenin o aya ait
/// ödeme kaydı varsa yeniden hesap ister.
/// </summary>
/// <remarks>
/// Önceden <c>PaymentSaga.StartAsync</c> doğrudan <c>AttendanceMarked</c> ile başlıyor ve her
/// çağrıda <c>Guid.NewGuid()</c> ürettiği için aynı öğrenci/ay'a onlarca saga + ödeme kaydı
/// açıyordu (#62). Bu ara katman iki koruma getiriyor:
///
/// 1. Kimlik deterministik — <see cref="SalaryPeriodId"/> (sözleşme, ay) ikilisinden türetiliyor.
/// 2. Ödeme kaydı zaten varsa hiç tetiklenmiyor; böylece onay süreci ilerlemiş bir saga
///    (öğrenci onayladı, öğretmen onayladı) yeni bir devamsızlık girişiyle
///    <c>AwaitingReceipt</c>'e geri sarılmıyor.
///
/// <para>Anahtar #154 ile sözleşmeye taşındığı için devamsızlığın hangi döneme ait olduğu artık
/// TARİHTEN çözülür: o günü kapsayan sözleşme bulunur. Öğrenciden türetilseydi ay içi devirde
/// düzeltme yanlış işletmenin tutarını değiştirirdi.</para>
///
/// Kalan dar yarış: aynı anda işlenen iki <c>AttendanceMarked</c> ikisi de kaydı yok görüp komutu
/// yayınlayabilir. Kimlik deterministik olduğu için sonuç yine tek satırdır ve iki hesap da aynı
/// değerleri ürettiğinden zararsızdır — tehlikeli olan "ilerlemiş saga'yı sıfırlama" durumu
/// yukarıdaki kontrolle kapalıdır.
/// </remarks>
public static class SalaryTriggerConsumer
{
    public static async Task<RecalculateMonthlySalary?> Handle(
        AttendanceMarked @event, IQuerySession session)
    {
        var day = @event.Date.Date;

        // Devamsızlık gününü kapsayan sözleşme. Aynı öğrencide ay içinde iki sözleşme olabilir;
        // gün hangisine düşüyorsa onun dönemi güncellenir.
        var contract = await session.Query<ContractEmploymentView>()
            .Where(c => c.StudentId == @event.StudentId
                        && c.StartDate <= day
                        && (c.EndDate == null || c.EndDate >= day))
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync();

        if (contract is null) return null;

        var month = @event.Date.ToString("yyyy-MM");
        var salaryPeriodId = SalaryPeriodId.For(contract.Id, month);

        // Maaş dönemini artık devamsızlık AÇMIYOR — MonthlySalarySchedulerService ay sonunda
        // ayla kesişen sözleşmeler için açıyor (#63). Devamsızlık yalnız kesintiyi etkiler.
        // Ay sonu koşusundan önce gelen devamsızlıklar için henüz kayıt yoktur; o zaman
        // yapılacak bir şey yok, hesap ay sonunda zaten biriken tüm günlerle yapılacak.
        var existing = await session.LoadAsync<PaymentSummary>(salaryPeriodId);
        if (existing is null) return null;

        // Ay sonu hesabından SONRA gelen düzeltme/geç giriş: tutarı güncelle.
        return new RecalculateMonthlySalary(salaryPeriodId, @event.Date);
    }
}
