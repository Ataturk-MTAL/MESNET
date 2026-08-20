using Marten;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Services;

/// <summary>
/// Devamsızlık kaydı değiştiğinde maaşın yeniden hesaplanması gerekiyor mu — ve gerekiyorsa
/// hangi dönemin (#255).
///
/// <para><b>Neden ayrı bir tüketici DEĞİL:</b> tetikleyici, sayacı güncelleyen tüketicinin
/// <b>dönüşü</b> olmak zorundadır. <c>MultipleHandlerBehavior.Separated</c> altında Wolverine her
/// handler tipi için ayrı bir sticky local queue açar; aynı mesajı işleyen iki ayrı tüketici
/// arasında <b>sıra garantisi yoktur</b> ve eşzamanlı koşabilirler. Tetikleyici önce koşarsa
/// <c>PaymentSaga</c> henüz commit olmamış <c>StudentAbsenceView</c>'ı okur, kesintiyi eski
/// değerle hesaplar ve <c>PaymentSaga</c>'nın "tutar değişmediyse sus" kısayolu yüzünden dönem
/// <b>bir daha hiç</b> tetiklenmez — ne hata, ne dead letter, ne log.</para>
///
/// <para>Cascading dönüş bu tuzağı yapısal olarak kapatır: <c>AutoApplyTransactions()</c> ile
/// komut, görünümün yazıldığı <b>aynı Marten transaction'ında</b> outbox'a girer ve yalnız commit
/// sonrası salınır. Saga taze görünümü görmek zorundadır.</para>
/// </summary>
public static class SalaryRecalculationTrigger
{
    /// <summary>
    /// Verilen gün için yeniden hesap komutu — gerekmiyorsa <c>null</c>.
    /// </summary>
    /// <param name="day">
    /// Devamsızlığın <b>kendi günü</b>; onay/düzeltme anı DEĞİL. Ay bundan türetilir ve
    /// <c>SalaryPeriodId</c> (sözleşme, ay) hash'i olduğu için yanlış ay <b>başka bir dönem</b>
    /// demektir: yanlış dönem yeniden hesaplanır, sayımı değişmediği için sessizce <c>null</c>
    /// döner ve doğru ay hiç düzelmez.
    /// </param>
    public static async Task<RecalculateMonthlySalary?> ForDayAsync(
        IQuerySession session, Guid studentId, DateTime day)
    {
        // #154 öncesi yazılmış görünüm satırlarında tarih yoktur. Varsayılan tarihle devam etmek
        // hesabı 0001-01 dönemine yazmak olurdu; tetiklememek doğru davranıştır.
        if (day == default) return null;

        var date = day.Date;

        // Günü kapsayan sözleşme. Aynı öğrencide ay içinde iki sözleşme olabilir; gün hangisine
        // düşüyorsa onun dönemi güncellenir (#154).
        var contract = await session.Query<ContractEmploymentView>()
            .Where(c => c.StudentId == studentId
                        && c.StartDate <= date
                        && (c.EndDate == null || c.EndDate >= date))
            .OrderByDescending(c => c.StartDate)
            .FirstOrDefaultAsync();

        if (contract is null) return null;

        var month = date.ToString("yyyy-MM");
        var salaryPeriodId = SalaryPeriodId.For(contract.Id, month);

        // Maaş dönemini devamsızlık AÇMAZ — MonthlySalarySchedulerService ay sonunda açar (#63).
        // Ay sonu koşusundan önceki devamsızlıklar için yapılacak bir şey yok; hesap zaten
        // biriken tüm günlerle yapılacak.
        var summary = await session.LoadAsync<PaymentSummary>(salaryPeriodId);
        if (summary is null) return null;

        // Kapanmış döneme komut gönderilmez: saga MarkCompleted ile SİLİNİR ve komut korele
        // olamayıp UnknownSagaException ile dead letter'a düşerdi. PaymentSummary saga'dan uzun
        // yaşadığı için bu kapı gereklidir.
        if (summary.Phase.IsFinal) return null;

        return new RecalculateMonthlySalary(salaryPeriodId, date);
    }
}
