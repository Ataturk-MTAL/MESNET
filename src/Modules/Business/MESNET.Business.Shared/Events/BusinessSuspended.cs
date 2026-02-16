namespace MESNET.Business.Shared.Events;

/// <summary>
/// İşletme pasife alındığında tetiklenir (belge ibraz edememe vs).
/// </summary>
public sealed record BusinessSuspended(
    Guid BusinessId,
    string SuspendedBy,
    string Reason,
    DateTime SuspendedAt);
