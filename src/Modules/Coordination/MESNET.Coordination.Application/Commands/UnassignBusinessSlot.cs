namespace MESNET.Coordination.Application.Commands;

/// <remarks>
/// İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan damgalar.
/// </remarks>
public sealed record UnassignBusinessSlot(
    Guid BusinessId,
    string Day,
    int PeriodNumber,
    Guid InstitutionId,
    string BranchCode = "",
    Guid AcademicPeriodId = default);
