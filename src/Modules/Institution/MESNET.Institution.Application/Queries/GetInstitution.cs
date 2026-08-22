using MESNET.Institution.Application.Security;
namespace MESNET.Institution.Application.Queries;

public sealed record GetInstitution(Guid InstitutionId) : IInstitutionScoped;
