using Marten;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Extensions;
using MESNET.Attendance.Application.Queries;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;

namespace MESNET.Attendance.Application.Handlers;

public static class ListAttendanceRecordsHandler
{
    public static async Task<PagedResult<AttendanceRecordDto>> Handle(
        ListAttendanceRecords query, IQuerySession session)
    {
        IQueryable<AttendanceRecord> queryable = session.Query<AttendanceRecord>()
            .Where(r => !r.IsDeleted);

        if (query.StudentId.HasValue)
            queryable = queryable.Where(r => r.StudentId == query.StudentId.Value);

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(r => r.BusinessId == query.BusinessId.Value);

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(r => r.InstitutionId == query.InstitutionId.Value);

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(r => r.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            AttendanceStatus.TryFromName(query.Status, true, out var attendanceStatus))
            queryable = queryable.Where(r => r.StatusName == attendanceStatus.Name);

        if (query.Year.HasValue)
            queryable = queryable.Where(r => r.Date.Year == query.Year.Value);

        if (query.Month.HasValue)
            queryable = queryable.Where(r => r.Date.Month == query.Month.Value);

        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: r => r.Date);

        return await queryable.ToPagedResultAsync(query, r => r.ToDto());
    }
}
