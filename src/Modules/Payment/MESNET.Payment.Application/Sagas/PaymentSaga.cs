using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using MESNET.Payment.Shared.Events;
using Wolverine;

namespace MESNET.Payment.Application.Sagas;

public class PaymentSaga : Saga
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public string Month { get; set; } = default!;
    public decimal BaseWage { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal GovernmentContribution { get; set; }
    public PaymentPhase Phase { get; set; } = PaymentPhase.Calculated;
    public Guid? ReceiptId { get; set; }
    public bool UploadedByStudent { get; set; }
    public DateTime ReceiptDueDate { get; set; }
    public bool StudentConfirmed { get; set; }
    public bool TeacherApproved { get; set; }
    public bool DeputyApproved { get; set; }

    // ─── START: Attendance'dan maaş hesaplama tetiklenir ───
    public static (PaymentSaga, SalaryCalculated, ReceiptUploadRequested) Start(
        AttendanceMarked @event,
        IDocumentSession session)
    {
        var salaryId = Guid.NewGuid();
        var month = @event.Date.ToString("yyyy-MM");

        // SalaryCalculationConfig'den parametreleri al
        var config = session.Query<SalaryCalculationConfig>()
            .Where(c => c.InstitutionId == @event.InstitutionId)
            .Where(c => c.EffectiveFrom <= @event.Date && (c.EffectiveTo == null || c.EffectiveTo >= @event.Date))
            .FirstOrDefault();

        // 3308 Madde 25 formülü (placeholder — gerçek implementasyonda business size, MEM durumu, devamsızlık hesabı yapılacak)
        decimal baseWage = config?.MinimumWage ?? 6631.40m;
        decimal deduction = 0m;             // TODO: Devamsızlık × günlük ücret
        decimal netAmount = baseWage - deduction;
        decimal govContrib = netAmount * 0.3333m;  // TODO: Devlet katkısı oranı (config'den)

        var saga = new PaymentSaga
        {
            Id = salaryId,
            StudentId = @event.StudentId,
            BusinessId = @event.BusinessId,
            InstitutionId = @event.InstitutionId,
            Month = month,
            BaseWage = baseWage,
            DeductionAmount = deduction,
            NetAmount = netAmount,
            GovernmentContribution = govContrib,
            Phase = PaymentPhase.AwaitingReceipt,
            ReceiptDueDate = new DateTime(@event.Date.Year, @event.Date.Month, 8, 23, 59, 59)
        };

        var salaryCalculated = new SalaryCalculated(
            salaryId, @event.StudentId, month, netAmount, baseWage, deduction, govContrib);

        var receiptUploadRequested = new ReceiptUploadRequested(
            salaryId, @event.StudentId, @event.BusinessId, saga.ReceiptDueDate);

        return (saga, salaryCalculated, receiptUploadRequested);
    }

    // ─── HANDLE: İşletme dekontu yükledi ───
    public void Handle(ReceiptUploadedByBusiness @event)
    {
        ReceiptId = @event.ReceiptId;
        Phase = PaymentPhase.ReceiptUploaded;
        UploadedByStudent = false;
    }

    // ─── HANDLE: Öğrenci dekontu yükledi (fallback — 8. günde işletme yüklemediyse) ───
    public void Handle(ReceiptUploadedByStudent @event)
    {
        ReceiptId = @event.ReceiptId;
        Phase = PaymentPhase.ReceiptUploaded;
        UploadedByStudent = true;
    }

    // ─── HANDLE: Öğrenci "aldım" dedi (1. adım — öğrenci parayı almalı) ───
    public void Handle(SalaryConfirmedByStudent @event)
    {
        if (Phase != PaymentPhase.ReceiptUploaded)
            throw new InvalidOperationException($"Geçersiz durum. Mevcut: {Phase.Slug}, Gerekli: {PaymentPhase.ReceiptUploaded.Slug}");

        StudentConfirmed = true;
        Phase = PaymentPhase.StudentConfirmed;
    }

    // ─── HANDLE: Koordinatör öğretmen onayladı (2. adım) ───
    public void Handle(ReceiptApprovedByTeacher @event)
    {
        if (!StudentConfirmed)
            throw new InvalidOperationException("Önce öğrenci maaşı aldığını onaylamalı.");

        if (Phase != PaymentPhase.StudentConfirmed)
            throw new InvalidOperationException($"Geçersiz durum. Mevcut: {Phase.Slug}, Gerekli: {PaymentPhase.StudentConfirmed.Slug}");

        TeacherApproved = true;
        Phase = PaymentPhase.TeacherApproved;
    }

    // ─── HANDLE: Müdür yardımcısı onayladı (3. adım — son yetkili) ───
    public PaymentCompleted Handle(ReceiptApprovedByDeputy @event)
    {
        if (!StudentConfirmed || !TeacherApproved)
            throw new InvalidOperationException("Öğrenci ve koordinatör öğretmen onayı gerekli.");

        if (Phase != PaymentPhase.TeacherApproved)
            throw new InvalidOperationException($"Geçersiz durum. Mevcut: {Phase.Slug}, Gerekli: {PaymentPhase.TeacherApproved.Slug}");

        DeputyApproved = true;
        Phase = PaymentPhase.Completed;
        MarkCompleted();

        return new PaymentCompleted(Id, StudentId, Month, NetAmount);
    }

    // ─── HANDLE: Dekont reddedildi ───
    public void Handle(ReceiptRejected @event)
    {
        ReceiptId = null;
        StudentConfirmed = false;
        TeacherApproved = false;
        DeputyApproved = false;
        Phase = PaymentPhase.Rejected;

        // Saga tamamlandı — yeni bir ödeme saga'sı gerekirse baştan başlar
        MarkCompleted();
    }

    // ─── TIMEOUT: Her ayın 8'inde dekont yüklenmediyse bildirim ───
    // Wolverine scheduled message ile implementasyon (Phase 2)
}
