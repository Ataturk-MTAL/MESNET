using Marten;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Application.Extensions;
using MESNET.Attendance.Application.Queries;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Handlers;

public static class GetAttendanceRecordHandler
{
    public static async Task<AttendanceRecordDto> Handle(
        GetAttendanceRecord query, IQuerySession session)
    {
        var record = await session.Events.AggregateStreamAsync<AttendanceRecord>(query.AttendanceId);
        if (record is null || record.IsDeleted)
            throw new DomainException(AttendanceErrors.NotFound(query.AttendanceId));

        return record.ToDto();
    }
}
