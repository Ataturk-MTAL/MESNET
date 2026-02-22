namespace MESNET.Contract.Shared.Events;

/// <summary>
/// İşletme yöneticisi fesih talebinde bulunduğunda tetiklenir.
/// Kurum müdürü/müdür yardımcısı tarafından onaylanması veya reddedilmesi beklenir.
/// </summary>
public sealed record ContractTerminationRequested(
    Guid ContractId,
    Guid StudentId,
    Guid BusinessId,
    string Reason,
    string ReasonType,
    string RequestedBy,
    DateTime RequestedAt);
