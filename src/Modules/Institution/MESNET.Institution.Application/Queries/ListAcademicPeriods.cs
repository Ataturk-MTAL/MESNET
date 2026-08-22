using MESNET.Common.Shared.Pagination;

using MESNET.Institution.Application.Security;
namespace MESNET.Institution.Application.Queries;

public sealed record ListAcademicPeriods(Guid InstitutionId) : PagedQuery, IInstitutionScoped;
