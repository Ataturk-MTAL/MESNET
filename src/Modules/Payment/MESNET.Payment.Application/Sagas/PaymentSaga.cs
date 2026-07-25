using Marten;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using MESNET.Payment.Shared.Events;
using Wolverine;
using Wolverine.Persistence.Sagas;

namespace MESNET.Payment.Application.Sagas;

public class PaymentSaga : Saga
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
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

    // ─── START: CalculateMonthlySalary ile maaş dönemi açılır ───
    // Giriş mesajı bilinçli olarak AttendanceMarked DEĞİL: doğrudan devamsızlık olayıyla
    // başlayınca her giriş yeni bir saga açıyordu (#62). Artık SalaryTriggerConsumer araya
    // giriyor, kimliği (öğrenci, ay) ikilisinden deterministik üretiyor ve kayıt zaten varsa
    // hiç tetiklemiyor. Aynı komut #63'teki aylık zamanlayıcının da giriş noktası olacak.
    //
    // Marten 9 senkron veri erişimini kaldırdı; .FirstOrDefault() burada
    // "As of Marten 9.0, only asynchronous data access is supported" fırlatıyordu (#73).
    // Birden çok cascading mesaj `OutgoingMessages` ile döndürülmeli — 3'lü tuple'da Wolverine
    // yalnız 1. elemanı saga olarak alıp diğerlerini sessizce düşürüyordu.
    // Async başlangıcın konvansiyondaki adı StartAsync.
    public static async Task<(PaymentSaga, OutgoingMessages)> StartAsync(
        CalculateMonthlySalary command,
        IDocumentSession session)
    {
        // SalaryCalculationConfig'den parametreleri al
        var config = await session.Query<SalaryCalculationConfig>()
            .Where(c => c.InstitutionId == command.InstitutionId)
            .Where(c => c.EffectiveFrom <= command.ReferenceDate
                        && (c.EffectiveTo == null || c.EffectiveTo >= command.ReferenceDate))
            .FirstOrDefaultAsync();

        // 3308 Madde 25 formülü (placeholder — işletme büyüklüğü, MESEM durumu ve devamsızlık
        // kesintisi hâlâ hesaba katılmıyor; taban ücret ham asgari ücret olarak alınıyor → #64)
        decimal baseWage = config?.MinimumWage ?? 6631.40m;
        decimal deduction = 0m;             // TODO: Devamsızlık × günlük ücret (#64)
        decimal netAmount = baseWage - deduction;
        decimal govContrib = netAmount * 0.3333m;  // TODO: Devlet katkısı oranı config'den (#64)

        var receiptDueDate = new DateTime(
            command.ReferenceDate.Year, command.ReferenceDate.Month, 8, 23, 59, 59);

        var saga = new PaymentSaga
        {
            Id = command.SalaryPeriodId,
            StudentId = command.StudentId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = command.AcademicPeriodId,
            Month = command.Month,
            BaseWage = baseWage,
            DeductionAmount = deduction,
            NetAmount = netAmount,
            GovernmentContribution = govContrib,
            Phase = PaymentPhase.AwaitingReceipt,
            ReceiptDueDate = receiptDueDate
        };

        var messages = new OutgoingMessages
        {
            new SalaryCalculated(
                command.SalaryPeriodId, command.StudentId, command.BusinessId,
                command.InstitutionId, command.AcademicPeriodId, command.Month,
                netAmount, baseWage, deduction, govContrib, receiptDueDate),
            new ReceiptUploadRequested(
                command.SalaryPeriodId, command.StudentId, command.BusinessId, receiptDueDate)
        };

        return (saga, messages);
    }

    // ─── HANDLE: İşletme dekontu yükledi ───
    // Saga korelasyonu: olaylar anahtarı `SalaryPeriodId` adıyla taşıyor. Wolverine'in varsayılan
    // konvansiyonu ({SagaTipi}Id / SagaId / Id) bu adı tanımadığı için korelasyon kurulamıyordu ve
    // saga'daki sıra kontrolleri hiç çalışmıyordu. Alternatif olan [SagaIdentity] attribute'ü olay
    // record'una konurdu — ama o zaman Payment.Shared'e WolverineFx bağımlılığı girer ve bu olayları
    // tüketen tüm modüllere yayılırdı. [SagaIdentityFrom] handler tarafında kalır, Shared temiz kalır.
    public void Handle(
        [SagaIdentityFrom(nameof(ReceiptUploadedByBusiness.SalaryPeriodId))] ReceiptUploadedByBusiness @event)
    {
        ReceiptId = @event.ReceiptId;
        Phase = PaymentPhase.ReceiptUploaded;
        UploadedByStudent = false;
    }

    // ─── HANDLE: Öğrenci dekontu yükledi (fallback — 8. günde işletme yüklemediyse) ───
    public void Handle(
        [SagaIdentityFrom(nameof(ReceiptUploadedByStudent.SalaryPeriodId))] ReceiptUploadedByStudent @event)
    {
        ReceiptId = @event.ReceiptId;
        Phase = PaymentPhase.ReceiptUploaded;
        UploadedByStudent = true;
    }

    // ─── HANDLE: Öğrenci "aldım" dedi (1. adım — öğrenci parayı almalı) ───
    public void Handle(
        [SagaIdentityFrom(nameof(SalaryConfirmedByStudent.SalaryPeriodId))] SalaryConfirmedByStudent @event)
    {
        if (Phase != PaymentPhase.ReceiptUploaded)
            throw new DomainException(PaymentErrors.InvalidPhase(Phase.Slug, PaymentPhase.ReceiptUploaded.Slug));

        StudentConfirmed = true;
        Phase = PaymentPhase.StudentConfirmed;
    }

    // ─── HANDLE: Koordinatör öğretmen onayladı (2. adım) ───
    public void Handle(
        [SagaIdentityFrom(nameof(ReceiptApprovedByTeacher.SalaryPeriodId))] ReceiptApprovedByTeacher @event)
    {
        if (!StudentConfirmed)
            throw new DomainException(PaymentErrors.ApprovalRequired("Öğrenci"));

        if (Phase != PaymentPhase.StudentConfirmed)
            throw new DomainException(PaymentErrors.InvalidPhase(Phase.Slug, PaymentPhase.StudentConfirmed.Slug));

        TeacherApproved = true;
        Phase = PaymentPhase.TeacherApproved;
    }

    // ─── HANDLE: Müdür yardımcısı onayladı (3. adım — son yetkili) ───
    public PaymentCompleted Handle(
        [SagaIdentityFrom(nameof(ReceiptApprovedByDeputy.SalaryPeriodId))] ReceiptApprovedByDeputy @event)
    {
        if (!StudentConfirmed)
            throw new DomainException(PaymentErrors.ApprovalRequired("Öğrenci"));

        if (!TeacherApproved)
            throw new DomainException(PaymentErrors.ApprovalRequired("Koordinatör öğretmen"));

        if (Phase != PaymentPhase.TeacherApproved)
            throw new DomainException(PaymentErrors.InvalidPhase(Phase.Slug, PaymentPhase.TeacherApproved.Slug));

        DeputyApproved = true;
        Phase = PaymentPhase.Completed;
        MarkCompleted();

        return new PaymentCompleted(Id, StudentId, Month, NetAmount);
    }

    // ─── HANDLE: Dekont reddedildi ───
    public void Handle(
        [SagaIdentityFrom(nameof(ReceiptRejected.SalaryPeriodId))] ReceiptRejected @event)
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
