using MESNET.Common.Infrastructure.Security;
using MESNET.Enrollment.Shared.Events;
using MESNET.Internship.Application.Commands;
using MESNET.Common.Shared;
using MESNET.Internship.Application.Errors;
using MESNET.Internship.Core.Enums;
using MESNET.Internship.Core.Policies;
using MESNET.Internship.Core.Services;
using MESNET.Internship.Core.ValueObjects;
using MESNET.Internship.Shared.Events;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MESNET.Internship.Application.Sagas;

public class InternshipSaga : Saga
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    /// <summary>
    /// İşletme — <b>okulda stajda null</b> (#159). Sözleşme akışı (fesih, tamamlama) bu hâlde
    /// hiç tetiklenmez: sözleşme kurulmaz, dolayısıyla o yolları besleyen olaylar gelmez.
    /// </summary>
    public Guid? BusinessId { get; set; }

    /// <summary>
    /// Sözleşme akışındaki olaylar için işletme kimliği. Okulda stajda bu yollara girilmez;
    /// girilirse sessizce boş kimlik yayınlamak yerine yüksek sesle patlar.
    /// </summary>
    private Guid BusinessIdForContractFlow => BusinessId
        ?? throw new InvalidOperationException(
            "Sözleşme akışı işletmesiz (okulda staj) yerleştirmede tetiklenemez — #159.");
    public Guid InstitutionId { get; set; }
    public Guid? ContractId { get; set; }
    public InternshipPhase Phase { get; set; } = InternshipPhase.Placed;
    public string? TerminationReason { get; set; }
    public string? TerminationReasonType { get; set; }
    public bool RequiresParentApproval { get; set; }
    public TerminationApprovalChain? ApprovalChain { get; set; }

    // ─── START: StudentPlaced event ile saga başlar ───
    public Guid PlacementId { get; set; }
    public Guid AcademicPeriodId { get; set; }

    public static (InternshipSaga, InternshipStarted) Start(StudentPlaced e)
    {
        // Kimlik yerleştirmeden DETERMİNİSTİK türer (#251). Guid.NewGuid() olsaydı aynı
        // StudentPlaced tekrar yayınlandığında ikinci bir saga daha doğardı — ölçüldü,
        // 2248 saga yalnız 95 yerleştirmeye karşılık geliyordu. Gerekçe: InternshipSagaId.
        var id = InternshipSagaId.For(e.PlacementId);
        var saga = new InternshipSaga
        {
            Id = id,
            PlacementId = e.PlacementId,
            StudentId = e.StudentId,
            BusinessId = e.BusinessId,
            InstitutionId = e.InstitutionId,
            AcademicPeriodId = e.AcademicPeriodId,
            // Okulda stajda sözleşme kurulmaz (#159): AwaitingContract'ta beklenirse saga
            // sonsuza kadar orada kalırdı. Staj fiilen sürüyor, doğrudan Active.
            Phase = e.BusinessId.HasValue ? InternshipPhase.AwaitingContract : InternshipPhase.Active
        };
        var started = new InternshipStarted(id, e.PlacementId, e.StudentId, e.StudentName, e.BusinessId, e.BusinessName, e.InstitutionId, e.AcademicPeriodId, DateTime.UtcNow);
        return (saga, started);
    }

    // ─── HANDLE: Sözleşme Aktifleşti ───
    //
    // Girdi Contract modülünün ContractActivated'ı DEĞİL, Internship'in kendi komutudur (#248).
    // Saga kimliği mesajın alan adından çözülür (InternshipId); başka modülün olayı o adı
    // taşımadığı için doğrudan bağlanamaz — IndeterminateSagaStateIdException ile ölü mektuba
    // düşerdi ve fiilen düşüyordu: 2248 saga'nın hiçbirinde contractId yazılı değildi.
    // Çeviriyi SagaRelayConsumer yapar.
    public void Handle(LinkInternshipContract e)
    {
        ContractId = e.ContractId;
        Phase = InternshipPhase.Active;
    }

    // Devamsızlık sınırı aşıldığında ayrı bir giriş noktası YOKTUR (#248): aktarıcı
    // InternshipTerminationRequested üretir ve manuel fesihle aynı yola girer. İkinci bir
    // giriş noktası ikinci bir sessiz kırılma yüzeyi demek olurdu.

    // ─── HANDLE: Manuel Fesih Talebi ───
    /// <summary>
    /// Fesih onay zincirini başlatır — <b>yalnız bir kez</b> (#252).
    ///
    /// <para><b>Tekrar eden talep durumu HİÇ değiştirmez.</b> Bu metot zinciri koşulsuz
    /// kuruyordu ve ikinci bir talep, toplanmış öğretmen / müdür yardımcısı / müdür onaylarını
    /// sessizce siliyordu. Talep iki yoldan da tekrar gelebilir: aktarıcı her
    /// <c>AttendanceLimitExceeded</c>'de bir tane üretir — devamsızlık sayacı dönem içinde
    /// sıfırlanmadığı için sınır dolduktan sonraki her yeni ya da <b>onaylanan</b> kayıt
    /// yeniden tetikler — ve manuel uç ikinci kez çağrılabilir.</para>
    ///
    /// <para><b>Neden <c>throw</c> değil sessiz <c>null</c>:</b> bu mesaj cascading olarak,
    /// durable local queue üzerinden <b>asenkron</b> işlenir; uç çoktan 200 dönmüştür.
    /// <c>DomainException</c> kullanıcıya 422 olarak ulaşmaz, yalnız mesajı dead letter'a
    /// düşürürdü. Karar saf <see cref="TerminationChainPolicy.CanStart"/> içindedir.</para>
    /// </summary>
    public InternshipTerminationApprovalChainStarted? Handle(
        InternshipTerminationRequested e, ILogger logger)
    {
        if (!TerminationChainPolicy.CanStart(ApprovalChain))
        {
            logger.LogInformation(
                "Fesih talebi yok sayıldı — onay zinciri zaten yürüyor. Staj: {SagaId}, "
                + "öğrenci: {StudentId}, yeni gerekçe: {Reason} ({ReasonType}).",
                Id, StudentId, e.Reason, e.ReasonType);
            return null;
        }

        Phase = InternshipPhase.TerminationInProgress;
        TerminationReason = e.Reason;
        TerminationReasonType = e.ReasonType;
        RequiresParentApproval = false;
        ApprovalChain = new TerminationApprovalChain();

        // StudentId saga'nın kendi state'inden (trigger event'i taşımaz) — Start'ta set edilir.
        return new InternshipTerminationApprovalChainStarted(Id, StudentId, RequiresParentApproval);
    }

    // ─── HANDLE: Onay Zinciri — Koordinatör Öğretmen ───
    public object? Handle(ApproveTerminationByTeacher e) =>
        Approve(TerminationStep.Teacher, c => c with { TeacherApproved = true });

    // ─── HANDLE: Onay Zinciri — Müdür Yardımcısı ───
    public object? Handle(ApproveTerminationByDeputy e) =>
        Approve(TerminationStep.Deputy, c => c with { DeputyApproved = true });

    // ─── HANDLE: Onay Zinciri — Müdür ───
    public object? Handle(ApproveTerminationByDirector e) =>
        Approve(TerminationStep.Director, c => c with { DirectorApproved = true });

    /// <summary>
    /// Bir adımı onaylar. <b>Sıra dayatılır</b> (#218): müdür yardımcısı, öğretmen onaylamadan
    /// onaylayamaz.
    ///
    /// <para>Karar saf <see cref="TerminationChainPolicy"/> içinde; burada yalnız uygulaması var.
    /// Sıra atlanırsa <c>DomainException</c> (422) fırlar ve mesaj <b>hangi adımın beklendiğini</b>
    /// söyler — yoksa kullanıcı neyi beklediğini bilemez.</para>
    /// </summary>
    private object? Approve(
        TerminationStep step, Func<TerminationApprovalChain, TerminationApprovalChain> apply)
    {
        if (ApprovalChain is null)
            throw new DomainException(InternshipErrors.TerminationNotStarted(Id));

        if (!TerminationChainPolicy.CanApprove(ApprovalChain, step))
            throw new DomainException(InternshipErrors.TerminationStepOutOfOrder(
                TerminationChainPolicy.DescribeOutOfOrder(ApprovalChain, step)));

        ApprovalChain = apply(ApprovalChain);
        return CheckApprovalChainComplete();
    }

    // ─── HANDLE: Override — Müdür Yardımcısı onay zincirini atlayabilir ───
    public OutgoingMessages Handle(OverrideTerminationApproval e)
    {
        ApprovalChain = ApprovalChain! with
        {
            IsOverridden = true,
            OverriddenBy = e.OverriddenBy,
            OverriddenAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        // Override da zinciri kapatır — fesih kesinleşir, öğrenci okula alınır (#220).
        var messages = TerminationCompletedMessages();
        messages.Add(new TerminationApprovalOverridden(
            Id, StudentId, e.OverriddenBy, e.Reason, DateTime.UtcNow));
        return messages;
    }

    // ─── HANDLE: Sözleşme Feshedildi ───
    // Girdi Contract modülünün olayı değil, aktarıcının ürettiği komuttur (#248) — bkz. yukarısı.
    public InternshipReplacementRequested Handle(TerminateInternshipContract e)
    {
        Phase = InternshipPhase.Terminated;
        MarkCompleted();

        return new InternshipReplacementRequested(
            StudentId, BusinessIdForContractFlow, InstitutionId, e.Reason);
    }

    // ─── HANDLE: Sözleşme Tamamlandı ───
    // Girdi Contract modülünün olayı değil, aktarıcının ürettiği komuttur (#248).
    public InternshipCompleted Handle(CompleteInternshipContract e)
    {
        Phase = InternshipPhase.Completed;
        MarkCompleted();

        return new InternshipCompleted(Id, StudentId, BusinessIdForContractFlow, DateTime.UtcNow);
    }

    // ─── PRIVATE: Onay zinciri kontrolü ───
    /// <summary>
    /// Zincir kapandıysa iki olay üretir: ıslak imza formu isteği ve <b>fesih kesinleşti</b>
    /// bildirimi (#220). İkincisini Enrollment tüketip öğrenciyi okula alır.
    /// </summary>
    private OutgoingMessages? CheckApprovalChainComplete()
    {
        if (!ApprovalChain!.IsComplete()) return null;

        ApprovalChain = ApprovalChain with { CompletedAt = DateTime.UtcNow };
        return TerminationCompletedMessages();
    }

    private OutgoingMessages TerminationCompletedMessages() =>
    [
        new TerminationFormRequested(Id, StudentId, BusinessId, InstitutionId),
        new InternshipTerminationCompleted(Id, StudentId, InstitutionId, AcademicPeriodId, BusinessId)
    ];
}
