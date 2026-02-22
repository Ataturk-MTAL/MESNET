namespace MESNET.Contract.Application.Commands;

/// <summary>
/// Müdür/müdür yardımcısı işletmenin fesih talebini reddeder.
/// Sözleşme Active durumuna geri döner.
/// </summary>
public sealed record RejectTermination(
    Guid InternshipContractId,
    string RejectedBy,
    string? RejectionNote = null);
