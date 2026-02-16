namespace MESNET.Payment.Application.Commands;

public sealed record UpdateMinimumWage(
    Guid InstitutionId,
    decimal NewMinimumWage,
    DateTime EffectiveFrom,
    string UpdatedBy);
