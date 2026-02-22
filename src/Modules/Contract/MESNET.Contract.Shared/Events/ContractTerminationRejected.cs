namespace MESNET.Contract.Shared.Events;

/// <summary>
/// Müdür/müdür yardımcısı işletmenin fesih talebini reddettiğinde tetiklenir.
/// Sözleşme Active durumuna geri döner.
/// </summary>
public sealed record ContractTerminationRejected(
    Guid ContractId,
    Guid StudentId,
    Guid BusinessId,
    string RejectedBy,
    string? RejectionNote,
    DateTime RejectedAt);
