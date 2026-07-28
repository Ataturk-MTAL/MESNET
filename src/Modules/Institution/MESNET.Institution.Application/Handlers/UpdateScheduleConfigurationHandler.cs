using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class UpdateScheduleConfigurationHandler
{
    private const int MinDailyPeriods = 1;
    private const int MaxDailyPeriods = 12;

    public static async Task<InstitutionUpdated> Handle(
        UpdateScheduleConfiguration command,
        IDocumentSession session,
        ICurrentUserService currentUser,
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
            // Aktör token'dan gelir, istekten DEĞİL (#137).
            UpdatedById = currentUser.GetUserId()
        };

        session.Store(institution);
        await session.SaveChangesAsync(cancellationToken);

        // Coordination'ın InstitutionView'ı güncel kalsın (DailyPeriodCount değişti)
        return new InstitutionUpdated(
            institution.Id, institution.FullName, institution.Location,
            institution.ScheduleConfig.DailyPeriodCount);
    }
}
