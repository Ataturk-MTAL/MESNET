using MESNET.Institution.Application.Security;
namespace MESNET.Institution.Application.Commands;

public sealed record DeactivateBranch(
    Guid InstitutionId,
    string FieldCode) : IInstitutionScoped;
