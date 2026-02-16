namespace MESNET.Institution.Application.Commands;

public sealed record UpdateMinimumWage(
    Guid InstitutionId,
    decimal Amount,
    DateTime EffectiveDate);
