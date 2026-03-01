using MESNET.Common.Shared.Pagination;

namespace MESNET.Business.Application.Queries;

public sealed record GetBusinessesByStatus(string? Status, string? Sector = null) : PagedQuery;
