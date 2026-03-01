using MESNET.Common.Shared.Pagination;

namespace MESNET.Coordination.Application.Queries;

public sealed record ListBusinessEvaluations(
    Guid? BusinessId,
    Guid? InstitutionId) : PagedQuery;
