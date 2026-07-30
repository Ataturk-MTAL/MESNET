namespace MESNET.Payment.Application.Commands;

/// <remarks>
/// <para>İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan damgalar.</para>
///
/// <para>Kurum kimliği de TAŞINMAZ (#147): parametre ulusaldır, kurum kapsamı yoktur. Eskiden
/// <c>InstitutionId</c> istek gövdesinden geliyordu ve yetkili bir kullanıcı başka kurumun
/// ücretini değiştirebiliyordu.</para>
/// </remarks>
public sealed record UpdateMinimumWage(
    decimal NewMinimumWage,
    /// <summary>16 yaşından küçükler için asgari ücret; null ise yaş ayrımı yapılmaz (#85).</summary>
    decimal? NewMinimumWageUnder16,
    DateTime EffectiveFrom);
