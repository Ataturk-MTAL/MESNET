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
        contract.CreatedAt);

    public static SignatureStatusDto ToDto(this SignatureStatus status) => new(
        status.IsSigned,
        status.SignedBy,
        status.SignedAt);
}
