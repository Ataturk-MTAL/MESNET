using Marten;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Application.Extensions;
using MESNET.Attendance.Application.Helpers;
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

        // Aktör adı saklanmaz, okuma anında çözülür (#139).
        var names = await UserNameResolver.ResolveAsync(session, record.ActorIds());

        return record.ToDto(names);
    }
}
