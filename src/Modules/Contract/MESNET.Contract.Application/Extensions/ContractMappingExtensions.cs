using MESNET.Contract.Application.Dtos;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.ValueObjects;

namespace MESNET.Contract.Application.Extensions;

public static class ContractMappingExtensions
{
    public static InternshipContractDto ToDto(this InternshipContract contract) => new(
        contract.Id,
        contract.StudentId,
        contract.BusinessId,
        contract.InstitutionId,
        contract.TeacherId,
        contract.Status.Name,
        contract.Status.Slug,
        contract.StartDate,
        contract.EndDate,
        contract.InstitutionSignature.ToDto(),
        contract.BusinessSignature.ToDto(),
        contract.StudentSignature.ToDto(),
        contract.TerminationReason,
        contract.TerminationReasonType?.Name,
        contract.TerminationReasonType?.Slug,
        contract.Documents.Select(d => d.ToDto()).ToList(),
        contract.CreatedAt);

    public static SignatureStatusDto ToDto(this SignatureStatus status) => new(
        status.IsSigned,
        status.SignedBy,
        status.SignedAt);

    public static ContractDocumentDto ToDto(this ContractDocument doc) => new(
        doc.DocumentId,
        doc.DocumentType,
        doc.DocumentTypeSlug,
        doc.Description,
        doc.ObjectPath,
        doc.UploadedBy,
        doc.UploadedAt);
}
