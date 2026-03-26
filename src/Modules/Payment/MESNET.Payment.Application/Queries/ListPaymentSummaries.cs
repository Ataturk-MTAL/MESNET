using MESNET.Common.Shared.Pagination;

namespace MESNET.Payment.Application.Queries;

public sealed record ListPaymentSummaries(
    Guid? StudentId,
    Guid? BusinessId,
    Guid? InstitutionId,
    Guid? AcademicPeriodId,
    string? Phase,
    string? Month,
    string? BranchCode = null,
    string? MonthFrom = null,
    string? MonthTo = null) : PagedQuery;
