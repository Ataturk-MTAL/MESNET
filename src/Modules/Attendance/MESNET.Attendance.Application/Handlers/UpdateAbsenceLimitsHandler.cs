using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Entities;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Handlers;

/// <summary>
/// Devamsızlık sınırlarını yazar (#183). Tek satırlık ulusal parametre — sürüm geçmişi yok,
/// çünkü sınır devamsızlık girildiği AN değerlendirilir (asgari ücretin aksine geriye dönük
/// hesap yoktur).
/// </summary>
public static class UpdateAbsenceLimitsHandler
{
    public static async Task Handle(
        UpdateAbsenceLimits command, IDocumentSession session, ICurrentUserService currentUser)
    {
        // Sıfır ya da negatif sınır "her öğrenci ilk devamsızlıkta feshedilir" demektir.
        // Bu bir fesih tetikleyicisi; yapılandırma onu sessizce sıfıra çekememeli.
        if (command.FormalUnexcusedDayLimit <= 0
            || command.FormalTotalDayLimit <= 0
            || command.MesemTotalDayLimit <= 0)
            throw new DomainException(AttendanceErrors.InvalidAbsenceLimit());

        // Toplam ayak mazeretsiz ayağı KAPSAR: her mazeretsiz gün aynı zamanda toplam bir gündür.
        // Toplamı mazeretsizin altına çekmek, mazeretsiz eşiğini erişilemez kılardı — o ayak
        // sessizce ölür ve idare 10 günlük sınırı uyguladığını sanmaya devam ederdi (#183).
        if (command.FormalTotalDayLimit < command.FormalUnexcusedDayLimit)
            throw new DomainException(AttendanceErrors.TotalLimitBelowUnexcused(
                command.FormalTotalDayLimit, command.FormalUnexcusedDayLimit));

        var config = await session.LoadAsync<AttendanceLimitConfig>(AttendanceLimitConfig.SingletonId)
                     ?? new AttendanceLimitConfig();

        config.FormalUnexcusedDayLimit = command.FormalUnexcusedDayLimit;
        config.FormalTotalDayLimit = command.FormalTotalDayLimit;
        config.MesemTotalDayLimit = command.MesemTotalDayLimit;
        config.UpdatedById = currentUser.GetUserId();
        config.UpdatedAt = DateTime.UtcNow;

        session.Store(config);
    }
}
