namespace MESNET.Business.Shared.Events;

/// <summary>
/// Bir okul işletmenin kapandığını bildirdi (#151). <b>Okula özeldir ve birikir</b>; işletmenin
/// küresel durumunu tek başına değiştirmez — o karar yeter sayıya ulaşınca
/// <see cref="BusinessClosed"/> ile verilir.
/// </summary>
public sealed record BusinessClosureReported(
    Guid BusinessId,
    Guid InstitutionId,
    Guid ReportedById,
    string? Reason,
    int ReportingInstitutionCount);
