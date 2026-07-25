namespace MESNET.Payment.Application.Commands;

public sealed record UpdateMinimumWage(
    Guid InstitutionId,
    decimal NewMinimumWage,
    /// <summary>16 yaşından küçükler için asgari ücret; null ise yaş ayrımı yapılmaz (#85).</summary>
    decimal? NewMinimumWageUnder16,
    DateTime EffectiveFrom,
    string UpdatedBy);
