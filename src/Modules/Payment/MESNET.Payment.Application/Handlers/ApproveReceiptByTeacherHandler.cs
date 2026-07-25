using Marten;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Handlers;

public static class ApproveReceiptByTeacherHandler
{
    public static async Task<ReceiptApprovedByTeacher> Handle(ApproveReceiptByTeacher command, IQuerySession session)
    {
        // ReceiptId, command'da taşınmaz; ilgili maaş döneminin PaymentSummary document'ından okunur.
        // (Düz Guid parametresi Wolverine codegen tarafından DI'dan çözülmeye çalışılıyordu → 500.)
        var summary = await session.LoadAsync<PaymentSummary>(command.SalaryPeriodId);
        if (summary is null)
            throw new DomainException(PaymentErrors.NotFound(command.SalaryPeriodId));
        if (summary.ReceiptId is not { } receiptId)
            throw new DomainException(PaymentErrors.ApprovalRequired("Koordinatör öğretmen"));

        // 2. adım: öğrenci maaşı aldığını onaylamadan öğretmen onaylayamaz (bkz. #72).
        if (summary.Phase != PaymentPhase.StudentConfirmed)
            throw new DomainException(PaymentErrors.InvalidPhase(
                summary.Phase.Slug, PaymentPhase.StudentConfirmed.Slug));

        return new ReceiptApprovedByTeacher(command.SalaryPeriodId, receiptId, command.ApprovedBy, DateTime.UtcNow);
    }
}
