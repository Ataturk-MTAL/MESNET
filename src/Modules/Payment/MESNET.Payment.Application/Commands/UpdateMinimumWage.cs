namespace MESNET.Payment.Application.Commands;

/// <remarks>
/// İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan damgalar.
/// </remarks>
public sealed record UpdateMinimumWage(
    Guid InstitutionId,
    decimal NewMinimumWage,
    /// <summary>16 yaşından küçükler için asgari ücret; null ise yaş ayrımı yapılmaz (#85).</summary>
    decimal? NewMinimumWageUnder16,
    DateTime EffectiveFrom);
