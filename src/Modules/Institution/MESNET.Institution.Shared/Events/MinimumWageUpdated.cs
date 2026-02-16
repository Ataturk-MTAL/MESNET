namespace MESNET.Institution.Shared.Events;

public sealed record MinimumWageUpdated(Guid InstitutionId, decimal Amount, DateTime EffectiveDate);
