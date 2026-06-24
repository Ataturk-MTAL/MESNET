using MESNET.Common.Shared;

namespace MESNET.Institution.Shared.Events;

public sealed record InstitutionUpdated(Guid InstitutionId, string FullName, Location? Location, int DailyPeriodCount);
