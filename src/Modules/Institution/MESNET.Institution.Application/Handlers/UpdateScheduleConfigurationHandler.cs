using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;

namespace MESNET.Institution.Application.Handlers;

public static class UpdateScheduleConfigurationHandler
{
    private const int MinDailyPeriods = 1;
    private const int MaxDailyPeriods = 12;

    public static async Task Handle(
        UpdateScheduleConfiguration command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId, cancellationToken);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

        if (command.DailyPeriodCount < MinDailyPeriods || command.DailyPeriodCount > MaxDailyPeriods)
            throw new DomainException(InstitutionErrors.InvalidPeriodCount(command.DailyPeriodCount, MinDailyPeriods, MaxDailyPeriods));

        institution.ScheduleConfig = new Core.Entities.ScheduleConfiguration
        {
            DailyPeriodCount = command.DailyPeriodCount,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = command.UpdatedBy
        };

        session.Store(institution);
        await session.SaveChangesAsync(cancellationToken);
    }
}
