using Marten;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class ApproveReceiptByDeputyHandler
{
    public static async Task<ReceiptApprovedByDeputy> Handle(ApproveReceiptByDeputy command, IQuerySession session)
    {
        // ReceiptId, command'da taşınmaz; ilgili maaş döneminin PaymentSummary document'ından okunur.
        // (Düz Guid parametresi Wolverine codegen tarafından DI'dan çözülmeye çalışılıyordu → 500.)
        var summary = await session.LoadAsync<PaymentSummary>(command.SalaryPeriodId);
        if (summary is null)
            throw new DomainException(PaymentErrors.NotFound(command.SalaryPeriodId));
        if (summary.ReceiptId is not { } receiptId)
            throw new DomainException(PaymentErrors.ApprovalRequired("Müdür yardımcısı"));

        return new ReceiptApprovedByDeputy(command.SalaryPeriodId, receiptId, command.ApprovedBy, DateTime.UtcNow);
    }
}
