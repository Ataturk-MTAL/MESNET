using Marten;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using MESNET.Payment.Core.ReadModels;
using MESNET.Payment.Shared.Events;

namespace MESNET.Payment.Application.Consumers;

// Sınıf adı Handler veya Consumer ile BİTMELİ — Wolverine tip keşfi konvansiyonu bu.
// Eski adı `PaymentSummaryUpdater` idi; hiç keşfedilmiyordu, dolayısıyla buradaki 8 Handle
// metodunun hiçbiri çalışmıyordu ve PaymentSummary hiç oluşmuyordu. Hata sessizdi:
// tüketicisi olmayan olay dead letter da üretmiyor.
public static class PaymentSummaryConsumer
{
    public static async Task Handle(SalaryCalculated @event, IDocumentSession session)
    {
        var profile = await session.LoadAsync<StudentPaymentProfile>(@event.StudentId);

        var summary = new PaymentSummary
        {
            Id = @event.SalaryPeriodId,
            StudentId = @event.StudentId,
            // BusinessId/InstitutionId/ReceiptDueDate atanmadığı sürece özet kaydı boş Guid ve
            // null tarihle yazılıyordu; işletme/kurum filtreleri ve son-gün indeksi ölüydü (#74).
            BusinessId = @event.BusinessId,
            InstitutionId = @event.InstitutionId,
            AcademicPeriodId = @event.AcademicPeriodId,
            Month = @event.Month,
            BaseWage = @event.BaseWage,
            DeductionAmount = @event.Deduction,
            NetAmount = @event.NetAmount,
            GovernmentContribution = @event.GovContribution,
            EmployerPayment = @event.NetAmount - @event.GovContribution,
            ReceiptDueDate = @event.ReceiptDueDate,
            Phase = PaymentPhase.Calculated,
            LastUpdated = DateTime.UtcNow,
            StudentName = profile?.FullName ?? "",
            StudentNumber = profile?.StudentNumber ?? "",
            BranchCode = profile?.BranchCode ?? "",
        };
        session.Store(summary);
    }

    public static async Task Handle(ReceiptUploadedByBusiness @event, IDocumentSession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(@event.SalaryPeriodId);
        if (summary is null) return;

        summary.ReceiptId = @event.ReceiptId;
        summary.ReceiptObjectPath = @event.ObjectPath;
        summary.UploadedByStudent = false;
        summary.Phase = PaymentPhase.ReceiptUploaded;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(ReceiptUploadedByStudent @event, IDocumentSession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(@event.SalaryPeriodId);
        if (summary is null) return;

        summary.ReceiptId = @event.ReceiptId;
        summary.ReceiptObjectPath = @event.ObjectPath;
        summary.UploadedByStudent = true;
        summary.Phase = PaymentPhase.ReceiptUploaded;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(SalaryConfirmedByStudent @event, IDocumentSession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(@event.SalaryPeriodId);
        if (summary is null) return;

        summary.Phase = PaymentPhase.StudentConfirmed;
        summary.StudentConfirmedAt = @event.ConfirmedAt;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(ReceiptApprovedByTeacher @event, IDocumentSession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(@event.SalaryPeriodId);
        if (summary is null) return;

        summary.Phase = PaymentPhase.TeacherApproved;
        summary.TeacherApprovedAt = @event.ApprovedAt;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(ReceiptApprovedByDeputy @event, IDocumentSession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(@event.SalaryPeriodId);
        if (summary is null) return;

        summary.Phase = PaymentPhase.DeputyApproved;
        summary.DeputyApprovedAt = @event.ApprovedAt;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(PaymentCompleted @event, IDocumentSession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(@event.SalaryPeriodId);
        if (summary is null) return;

        summary.Phase = PaymentPhase.Completed;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }

    public static async Task Handle(ReceiptRejected @event, IDocumentSession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(@event.SalaryPeriodId);
        if (summary is null) return;

        summary.Phase = PaymentPhase.Rejected;
        summary.ReceiptId = null;
        summary.ReceiptObjectPath = null;
        summary.StudentConfirmedAt = null;
        summary.TeacherApprovedAt = null;
        summary.DeputyApprovedAt = null;
        summary.LastUpdated = DateTime.UtcNow;
        session.Store(summary);
    }
}
