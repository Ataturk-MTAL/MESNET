using Marten;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Queries;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Policies;

namespace MESNET.Attendance.Application.Handlers;

public static class GetAbsenceLimitsHandler
{
    public static async Task<AbsenceLimitsDto> Handle(GetAbsenceLimits _, IQuerySession session)
    {
        var config = await session.LoadAsync<AttendanceLimitConfig>(AttendanceLimitConfig.SingletonId);

        // Değerler politikadan okunur, config'ten DEĞİL: bozuk/eksik kayıtta hangi sınırın fiilen
        // uygulandığını göstermek gerekir. Arayüzün gösterdiği sayı ile handler'ın uyguladığı
        // sayı ayrışırsa idare yanlış sınıra göre karar verir.
        var limits = AttendanceLimitPolicy.EffectiveLimits(config);

        return new AbsenceLimitsDto(
            limits.FormalUnexcusedDayLimit,
            limits.FormalTotalDayLimit,
            limits.MesemTotalDayLimit,
            config is not null,
            config?.UpdatedAt);
    }
}
