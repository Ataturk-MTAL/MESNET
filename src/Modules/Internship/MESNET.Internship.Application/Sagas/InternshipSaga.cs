using MESNET.Attendance.Shared.Events;
using MESNET.Contract.Shared.Events;
using MESNET.Enrollment.Shared.Events;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Core.Enums;
using MESNET.Internship.Core.ValueObjects;
using MESNET.Internship.Shared.Events;
using Wolverine;

namespace MESNET.Internship.Application.Sagas;

public class InternshipSaga : Saga
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
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
            Phase = InternshipPhase.AwaitingContract
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

    // ─── HANDLE: Onay Zinciri — Veli ───
    public object? Handle(ApproveTerminationByParent e)
    {
        ApprovalChain = ApprovalChain! with { ParentApproved = true };
        return CheckApprovalChainComplete();
    }

    // ─── HANDLE: Onay Zinciri — Koordinatör Öğretmen ───
    public object? Handle(ApproveTerminationByTeacher e)
    {
        ApprovalChain = ApprovalChain! with { TeacherApproved = true };
        return CheckApprovalChainComplete();
    }

    // ─── HANDLE: Onay Zinciri — Müdür Yardımcısı ───
    public object? Handle(ApproveTerminationByDeputy e)
    {
        ApprovalChain = ApprovalChain! with { DeputyApproved = true };
        return CheckApprovalChainComplete();
    }

    // ─── HANDLE: Onay Zinciri — Müdür ───
    public object? Handle(ApproveTerminationByDirector e)
    {
        ApprovalChain = ApprovalChain! with { DirectorApproved = true };
        return CheckApprovalChainComplete();
    }

    // ─── HANDLE: Onay Zinciri — İşletme Yetkilisi ───
    public object? Handle(ApproveTerminationByBusinessRep e)
    {
        ApprovalChain = ApprovalChain! with { BusinessRepApproved = true };
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
            StudentId, BusinessId, InstitutionId, string.Empty);
    }

    // ─── HANDLE: Sözleşme Tamamlandı ───
    public InternshipCompleted Handle(ContractCompleted e)
    {
        Phase = InternshipPhase.Completed;
        MarkCompleted();

        return new InternshipCompleted(Id, StudentId, BusinessId, DateTime.UtcNow);
    }

    // ─── PRIVATE: Onay zinciri kontrolü ───
    private TerminationFormRequested? CheckApprovalChainComplete()
    {
        if (ApprovalChain!.IsComplete(RequiresParentApproval))
        {
            ApprovalChain = ApprovalChain with { CompletedAt = DateTime.UtcNow };
            return new TerminationFormRequested(Id, StudentId, BusinessId, InstitutionId);
        }

        return null;
    }
}
