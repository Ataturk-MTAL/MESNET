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
    DateTime? EndDate,
    SignatureStatus InstitutionSignature,
    SignatureStatus BusinessSignature,
    SignatureStatus StudentSignature,
    SignatureStatus ParentSignature,
    string? TerminationReason,
    TerminationReason? TerminationReasonType,
    IReadOnlyList<ContractDocument> Documents,
    DateTime CreatedAt)
{
    public static InternshipContract Create(ContractCreated e) => new(
        e.ContractId, e.StudentId, e.BusinessId, e.InstitutionId, e.TeacherId,
        ContractStatus.Draft, e.StartDate, null,
        SignatureStatus.Unsigned, SignatureStatus.Unsigned, SignatureStatus.Unsigned, SignatureStatus.Unsigned,
        null, null, [], e.CreatedAt);

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
            EndDate = e.EndDate,
            TerminationReason = e.Reason,
            TerminationReasonType = Enums.TerminationReason.TryFromName(e.ReasonType, true, out var reason) ? reason : null
        };

    public InternshipContract Apply(ContractCompleted e)
        => this with { Status = ContractStatus.Completed, EndDate = e.EndDate };

    public InternshipContract Apply(ContractTerminationRequested e)
        => this with
        {
            Status = ContractStatus.TerminationRequested,
            TerminationReason = e.Reason,
            TerminationReasonType = Enums.TerminationReason.TryFromName(e.ReasonType, true, out var reason) ? reason : null
        };

    public InternshipContract Apply(ContractTerminationRejected _)
        => this with
        {
            Status = ContractStatus.Active,
            TerminationReason = null,
            TerminationReasonType = null
        };

    public InternshipContract Apply(ContractDocumentUploaded e)
    {
        var doc = new ContractDocument(
            e.DocumentId, e.DocumentType, e.DocumentTypeSlug,
            e.Description, e.ObjectPath, e.UploadedBy, e.UploadedAt);
        return this with { Documents = [.. Documents, doc] };
    }

    // Eski event'ler için geriye dönük uyumluluk (event store'da kayıtlı olabilir)
    public InternshipContract Apply(SignedContractDocumentUploaded e)
    {
        var doc = new ContractDocument(
            Guid.NewGuid(), "SignedContract", "Islak İmzalı Sözleşme",
            null, e.ObjectPath, e.UploadedBy, e.UploadedAt);
        return this with { Documents = [.. Documents, doc] };
    }

    public InternshipContract Apply(TerminationDocumentUploaded e)
    {
        var doc = new ContractDocument(
            Guid.NewGuid(), "TerminationLetter", "Fesih Belgesi",
            null, e.ObjectPath, e.UploadedBy, e.UploadedAt);
        return this with { Documents = [.. Documents, doc] };
    }

    public bool AllSignaturesComplete
        => InstitutionSignature.IsSigned && BusinessSignature.IsSigned
           && StudentSignature.IsSigned && ParentSignature.IsSigned;
}
