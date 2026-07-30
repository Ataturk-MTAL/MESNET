using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Enums;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Application.Messages;
using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using MESNET.Payment.Core.ReadModels;
using MESNET.Payment.Core.Services;
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
        var result = await CalculateAsync(command, session);

        // business-rules.md §6.6: ücret her ayın 8'ine kadar yatırılır — çalışılan ayın DEĞİL,
        // TAKİP EDEN ayın 8'i. Maaş ay sonunda hesaplandığı için referans ayın 8'i kullanılsaydı
        // son ödeme günü daha doğduğu anda geçmişte kalırdı (#63).
        var nextMonth = new DateTime(command.ReferenceDate.Year, command.ReferenceDate.Month, 1)
            .AddMonths(1);
        var receiptDueDate = new DateTime(nextMonth.Year, nextMonth.Month, 8, 23, 59, 59);

        var saga = new PaymentSaga
        {
            Id = command.SalaryPeriodId,
            StudentId = command.StudentId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            AcademicPeriodId = command.AcademicPeriodId,
            Month = command.Month,
            BaseWage = result.BaseWage,
            DeductionAmount = result.Deduction,
            NetAmount = result.NetAmount,
            GovernmentContribution = result.GovernmentContribution,
            Phase = PaymentPhase.AwaitingReceipt,
            ReceiptDueDate = receiptDueDate
        };

        var messages = new OutgoingMessages
        {
            new SalaryCalculated(
                command.SalaryPeriodId, command.StudentId, command.BusinessId,
                command.InstitutionId, command.AcademicPeriodId, command.Month,
                result.NetAmount, result.BaseWage, result.Deduction,
                result.GovernmentContribution, receiptDueDate),
            new ReceiptUploadRequested(
                command.SalaryPeriodId, command.StudentId, command.BusinessId, receiptDueDate)
        };

        // Son ödeme gününde dekont hâlâ yoksa uyarı gitsin (#69). ReceiptOverdueConsumer
        // tetiklendiğinde PaymentSummary'ye bakıp dekont gelmişse sessizce yutar —
        // zamanlanmış mesaj sonradan iptal edilemiyor.
        //
        // Geçmişte kalan son ödeme günü için zamanlama YAPILMAZ: Wolverine geçmiş tarihli
        // mesajı anında teslim eder, bu da geriye dönük seed edilen aylarda anlamsız
        // bildirim yağmuruna yol açardı.
        if (receiptDueDate > DateTime.UtcNow)
        {
            messages.Add(new ReceiptOverdue(
                    command.SalaryPeriodId, command.StudentId, command.BusinessId,
                    command.InstitutionId, command.Month, receiptDueDate)
                .ScheduledAt(new DateTimeOffset(receiptDueDate, TimeSpan.Zero)));
        }

        return (saga, messages);
    }

    // ─── HANDLE: Ay içinde yeni devamsızlık geldi, tutarı yeniden hesapla ───
    // Kesinti ancak ay boyunca biriken devamsızlıkla doğru olur. Tetikleyici ayın ilk
    // devamsızlığı olduğu için ilk hesap hep tek gün üzerinden çıkar; sonraki her giriş
    // bu handler'la tutarı günceller. Yalnız dekont beklenirken geçerli — onay süreci
    // başladıysa (dekont yüklendi, öğrenci/öğretmen onayladı) tutar dondurulur.
    public async Task<SalaryRecalculated?> Handle(
        [SagaIdentityFrom(nameof(RecalculateMonthlySalary.SalaryPeriodId))] RecalculateMonthlySalary command,
        IQuerySession session)
    {
        // Onay süreci başladıysa tutar dondurulur — dekont yüklenmiş bir ödemenin tutarı
        // sonradan değişirse öğrenci/öğretmen/müdür yrd. onayladıkları rakamdan başkasını almış olur.
        if (Phase != PaymentPhase.AwaitingReceipt) return null;

        var result = await CalculateAsync(
            new CalculateMonthlySalary(
                Id, StudentId, BusinessId, InstitutionId, AcademicPeriodId, Month, command.ReferenceDate),
            session);

        if (result.NetAmount == NetAmount && result.Deduction == DeductionAmount) return null;

        BaseWage = result.BaseWage;
        DeductionAmount = result.Deduction;
        NetAmount = result.NetAmount;
        GovernmentContribution = result.GovernmentContribution;

        return new SalaryRecalculated(
            Id, StudentId, Month,
            result.NetAmount, result.BaseWage, result.Deduction, result.GovernmentContribution);
    }

    private static async Task<SalaryCalculator.Result> CalculateAsync(
        CalculateMonthlySalary command, IQuerySession session)
    {
        // Yürürlük seçimi HESAPLANAN AYDAN türetilir, hesabın koştuğu andan değil — asgari ücret
        // yıl içinde birden fazla kez artabilir ve "şu an" ile seçim geçmiş aya yeni ücreti
        // uygular. Gerekçenin tamamı: SalaryMonth.ConfigReferenceDate.
        var configDate = SalaryMonth.ConfigReferenceDate(command.Month, command.ReferenceDate);

        var config = await session.Query<SalaryCalculationConfig>()
            .Where(c => c.InstitutionId == command.InstitutionId)
            .Where(c => c.EffectiveFrom <= configDate
                        && (c.EffectiveTo == null || c.EffectiveTo >= configDate))
            // Birden fazla kayıt eşleşirse en yenisi geçerlidir; sıralama olmadan hangisinin
            // geleceği belirsizdi ve tutar çalıştırmadan çalıştırmaya değişebilirdi (#75).
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync();

        // Config yoksa sessizce eski bir sabitle hesaplamak yanlış tutar üretir (#64) — hata ver.
        if (config is null)
            throw new DomainException(PaymentErrors.SalaryConfigMissing(command.InstitutionId));

        var business = await session.LoadAsync<BusinessPaymentProfile>(command.BusinessId);
        var student = await session.LoadAsync<StudentPaymentProfile>(command.StudentId);

        var deductibleDays = await CountDeductibleAbsenceDaysAsync(session, command.StudentId, command.Month);

        // Sözleşmede taahhüt edilen ücret (#84). Yürürlükte sözleşme yoksa veya ücret
        // belirtilmemişse null geçilir ve yasal taban uygulanır.
        var contractWage = await session.LoadAsync<StudentContractWageView>(command.StudentId);

        return SalaryCalculator.Calculate(
            config,
            business?.PersonnelCount ?? 0,
            student?.EducationTypeName ?? "",
            student?.ClassYear ?? 0,
            student?.HasJourneymanQualification ?? false,
            deductibleDays,
            contractWage is { IsActive: true } ? contractWage.AgreedMonthlyWage : null,
            CalculateAge(student?.BirthDate, command.ReferenceDate),
            IsApprenticeCategory(student?.CategoryName),
            // Kamu kurumuna devlet katkısı ödenmez (#157). Profil yoksa false — özel işletme
            // varsayımı bugünkü davranışı korur; eksik veri yüzünden katkı sessizce sıfırlanmaz.
            business?.IsPublicInstitution ?? false);
    }

    /// <summary>
    /// Öğrencinin hesap tarihindeki tam yaşı (#85). Doğum tarihi bilinmiyorsa null döner ve
    /// yaşa bakılmaksızın genel asgari ücret uygulanır.
    /// </summary>
    private static int? CalculateAge(DateTime? birthDate, DateTime referenceDate)
    {
        if (birthDate is not { } birth) return null;

        var age = referenceDate.Year - birth.Year;
        if (referenceDate.Month < birth.Month
            || (referenceDate.Month == birth.Month && referenceDate.Day < birth.Day))
            age--;

        return age < 0 ? null : age;
    }

    // SmartEnum Marten LINQ'te kullanılamadığı için read model düz string tutuyor.
    private static bool IsApprenticeCategory(string? categoryName)
        => StudentCategory.TryFromName(categoryName ?? "", ignoreCase: true, out var category)
           && category.IsApprentice;

    /// <summary>
    /// Onaylanmış, ücret kesintisine tabi devamsızlık günü sayısı — mazeretsiz devamsızlık ve
    /// ücretsiz izin (<c>AbsenceType.AffectsSalary</c>). <c>Pending</c> sayılmaz: işletmenin tek
    /// taraflı girişi öğretmen onayı olmadan öğrencinin ücretini kesemez.
    /// </summary>
    private static Task<int> CountDeductibleAbsenceDaysAsync(
        IQuerySession session, Guid studentId, string month)
        => session.Query<StudentAbsenceView>()
            .Where(a => a.StudentId == studentId
                        && a.Month == month
                        && DeductibleAbsenceTypes.Contains(a.AbsenceTypeName)
                        && a.StatusName != PendingStatus)
            .CountAsync();

    // AbsenceType.AffectsSalary ile aynı küme. SmartEnum Marten LINQ'te kullanılamadığı için
    // düz string (bkz. CLAUDE.md — SmartEnum LINQ kuralları).
    private static readonly string[] DeductibleAbsenceTypes = ["Unexcused", "UnpaidLeave"];
    private const string PendingStatus = "Pending";

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
