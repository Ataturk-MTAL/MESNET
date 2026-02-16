using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;

namespace MESNET.Institution.Application.Handlers;

public static class UpdateScheduleConfigurationHandler
{
    private const int MinDailyPeriods = 1;
    private const int MaxDailyPeriods = 12;

    public static async Task<Result> Handle(
        UpdateScheduleConfiguration command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        // 1. Kurum mevcut mu kontrol et
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId, cancellationToken);
        if (institution is null)
        {
            return Result.Failure(
                InstitutionErrors.NotFound(command.InstitutionId));
        }

        // 2. Period count validation
        if (command.DailyPeriodCount < MinDailyPeriods || command.DailyPeriodCount > MaxDailyPeriods)
        {
            return Result.Failure(
                InstitutionErrors.InvalidPeriodCount(command.DailyPeriodCount, MinDailyPeriods, MaxDailyPeriods));
        }

        // 3. ScheduleConfig oluştur veya güncelle
        institution.ScheduleConfig = new Core.Entities.ScheduleConfiguration
        {
            DailyPeriodCount = command.DailyPeriodCount,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = command.UpdatedBy
        };

        session.Store(institution);
        await session.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
