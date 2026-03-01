using MESNET.Common.Shared.Pagination;

namespace MESNET.Internship.Application.Queries;

public sealed record ListInternships(
    Guid? StudentId,
    Guid? BusinessId,
    Guid? InstitutionId,
    string? Phase) : PagedQuery;
