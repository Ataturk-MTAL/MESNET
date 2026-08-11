using MESNET.Institution.Application.Security;
namespace MESNET.Institution.Application.Commands;

public sealed record ActivateBranch(
    Guid InstitutionId,
    string FieldCode) : IInstitutionScoped;
