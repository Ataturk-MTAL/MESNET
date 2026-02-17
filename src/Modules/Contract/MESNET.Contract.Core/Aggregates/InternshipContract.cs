using MESNET.Contract.Core.Enums;
using MESNET.Contract.Core.ValueObjects;
using MESNET.Contract.Shared.Events;

namespace MESNET.Contract.Core.Aggregates;

public sealed record InternshipContract(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid? TeacherId,
    ContractStatus Status,
    DateTime StartDate,
    DateTime EndDate,
    SignatureStatus InstitutionSignature,
    SignatureStatus BusinessSignature,
    SignatureStatus StudentSignature,
    SignatureStatus ParentSignature,
    string? TerminationReason,
    TerminationReason? TerminationReasonType,
    string? SignedDocumentUrl,
    string? TerminationDocumentUrl,
    DateTime CreatedAt)
{
    public static InternshipContract Create(ContractCreated e) => new(
        e.ContractId, e.StudentId, e.BusinessId, e.InstitutionId, e.TeacherId,
        ContractStatus.Draft, e.StartDate, e.EndDate,
        SignatureStatus.Unsigned, SignatureStatus.Unsigned, SignatureStatus.Unsigned, SignatureStatus.Unsigned,
        null, null, null, null, e.CreatedAt);

    public InternshipContract Apply(ContractSubmittedForSignature _)
        => this with { Status = ContractStatus.AwaitingSignature };

    public InternshipContract Apply(ContractSignedByInstitution e)
        => this with { InstitutionSignature = SignatureStatus.Sign(e.SignedBy) };

    public InternshipContract Apply(ContractSignedByBusiness e)
        => this with { BusinessSignature = SignatureStatus.Sign(e.SignedBy) };

    public InternshipContract Apply(ContractSignedByStudent e)
        => this with { StudentSignature = SignatureStatus.Sign(e.SignedBy) };

    public InternshipContract Apply(ContractSignedByParent e)
        => this with { ParentSignature = SignatureStatus.Sign(e.SignedBy) };

    public InternshipContract Apply(ContractActivated _)
        => this with { Status = ContractStatus.Active };

    public InternshipContract Apply(ContractSuspended _)
        => this with { Status = ContractStatus.Suspended };

    public InternshipContract Apply(ContractResumed _)
        => this with { Status = ContractStatus.Active };

    public InternshipContract Apply(ContractTerminated e)
        => this with
        {
            Status = ContractStatus.Terminated,
            TerminationReason = e.Reason,
            TerminationReasonType = Enums.TerminationReason.TryFromName(e.ReasonType, true, out var reason) ? reason : null
        };

    public InternshipContract Apply(ContractCompleted _)
        => this with { Status = ContractStatus.Completed };

    public InternshipContract Apply(SignedContractDocumentUploaded e)
        => this with { SignedDocumentUrl = e.ObjectPath };

    public InternshipContract Apply(TerminationDocumentUploaded e)
        => this with { TerminationDocumentUrl = e.ObjectPath };

    public bool AllSignaturesComplete
        => InstitutionSignature.IsSigned && BusinessSignature.IsSigned
           && StudentSignature.IsSigned && ParentSignature.IsSigned;
}
