using MESNET.Common.Shared.Pagination;

namespace MESNET.Attendance.Application.Queries;

public sealed record ListAttendanceRecords(
    Guid? StudentId,
    Guid? BusinessId,
    Guid? InstitutionId,
    Guid? AcademicPeriodId,
    string? Status,
    int? Year,
    int? Month,
    string? BranchCode = null) : PagedQuery;
