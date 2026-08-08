using MESNET.Common.Infrastructure.Security;
using MESNET.Attendance.Shared.Events;
using MESNET.Contract.Shared.Events;
using MESNET.Enrollment.Shared.Events;
using MESNET.Internship.Application.Commands;
using MESNET.Common.Shared;
using MESNET.Internship.Application.Errors;
using MESNET.Internship.Core.Enums;
using MESNET.Internship.Core.Policies;
using MESNET.Internship.Core.ValueObjects;
using MESNET.Internship.Shared.Events;
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
        var id = Guid.NewGuid();
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
    public void Handle(ContractActivated e)
    {
        ContractId = e.ContractId;
        Phase = InternshipPhase.Active;
    }

    // ─── HANDLE: Devamsızlık Limiti Aşıldı → Otomatik Fesih Başlat ───
    public InternshipTerminationApprovalChainStarted Handle(AttendanceLimitExceeded e)
    {
        Phase = InternshipPhase.TerminationInProgress;
        TerminationReason = $"Devamsızlık limiti aşıldı: {e.TotalAbsenceDays}/{e.Limit} gün";
        TerminationReasonType = "AttendanceLimitExceeded";
        RequiresParentApproval = false;
        ApprovalChain = new TerminationApprovalChain();

        return new InternshipTerminationApprovalChainStarted(Id, StudentId, RequiresParentApproval);
    }

    // ─── HANDLE: Manuel Fesih Talebi ───
    public InternshipTerminationApprovalChainStarted Handle(InternshipTerminationRequested e)
    {
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
    public (TerminationApprovalOverridden, TerminationFormRequested) Handle(OverrideTerminationApproval e)
    {
        ApprovalChain = ApprovalChain! with
        {
            IsOverridden = true,
            OverriddenBy = e.OverriddenBy,
            OverriddenAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };

        return (
            new TerminationApprovalOverridden(Id, StudentId, e.OverriddenBy, e.Reason, DateTime.UtcNow),
            new TerminationFormRequested(Id, StudentId, BusinessId, InstitutionId)
        );
    }

    // ─── HANDLE: Sözleşme Feshedildi (Contract modülünden) ───
    public InternshipReplacementRequested Handle(ContractTerminated e)
    {
        Phase = InternshipPhase.Terminated;
        MarkCompleted();

        return new InternshipReplacementRequested(
            StudentId, BusinessIdForContractFlow, InstitutionId, string.Empty);
    }

    // ─── HANDLE: Sözleşme Tamamlandı ───
    public InternshipCompleted Handle(ContractCompleted e)
    {
        Phase = InternshipPhase.Completed;
        MarkCompleted();

        return new InternshipCompleted(Id, StudentId, BusinessIdForContractFlow, DateTime.UtcNow);
    }

    // ─── PRIVATE: Onay zinciri kontrolü ───
    private TerminationFormRequested? CheckApprovalChainComplete()
    {
        if (ApprovalChain!.IsComplete())
        {
            ApprovalChain = ApprovalChain with { CompletedAt = DateTime.UtcNow };
            return new TerminationFormRequested(Id, StudentId, BusinessId, InstitutionId);
        }

        return null;
    }
}
