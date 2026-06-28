using Marten;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Guards;

/// <summary>
/// Kapalı akademik dönem (salt okunur mod) guard'ı.
/// <see cref="IAttendancePeriodScoped"/> taşıyan tüm YAZMA command'larından önce çalışır:
/// hedef devamsızlık kaydını yükler, ait olduğu akademik dönem kapalıysa
/// (<see cref="AcademicPeriodView.IsActive"/> = false) <see cref="DomainException"/> fırlatır.
/// </summary>
public static class AttendancePeriodGuardMiddleware
{
    public static async Task BeforeAsync(IAttendancePeriodScoped message, IQuerySession session)
    {
        // Aggregate yoksa burada bir şey yapma — handler kendi NOT_FOUND (422) hatasını atsın.
        var record = await session.LoadAsync<AttendanceRecord>(message.AttendanceId);
        if (record is null) return;

        var period = await session.LoadAsync<AcademicPeriodView>(record.AcademicPeriodId);
        if (period is { IsActive: false })
            throw new DomainException(AttendanceErrors.AcademicPeriodClosed(record.AcademicPeriodId));
    }
}
