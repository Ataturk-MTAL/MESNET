using MESNET.Common.Shared.Pagination;

namespace MESNET.Institution.Application.Queries;

public sealed record ListAcademicPeriods(Guid InstitutionId) : PagedQuery;
