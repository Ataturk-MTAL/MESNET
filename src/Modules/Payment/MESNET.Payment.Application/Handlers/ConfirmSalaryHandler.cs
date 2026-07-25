using Marten;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class ConfirmSalaryHandler
{
    // Onay sırası ikinci kez burada doğrulanıyor. Tek koruma saga'da kalırsa, saga korelasyonu
    // bozulduğunda sıra sessizce zorlanmaz hale gelir (bkz. #72) — PaymentSummaryUpdater olayı
    // yine de yazar ve arayüz normal görünür. Bu kontrol o sessiz başarısızlığı kapatır.
    public static async Task<SalaryConfirmedByStudent> Handle(ConfirmSalary command, IQuerySession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(command.SalaryPeriodId);
        if (summary is null)
            throw new DomainException(PaymentErrors.NotFound(command.SalaryPeriodId));

        if (summary.Phase != PaymentPhase.ReceiptUploaded)
            throw new DomainException(PaymentErrors.InvalidPhase(
                summary.Phase.Slug, PaymentPhase.ReceiptUploaded.Slug));

        return new SalaryConfirmedByStudent(command.SalaryPeriodId, command.StudentId, DateTime.UtcNow);
    }
}
