namespace MESNET.Contract.Shared.Events;

/// <summary>
/// Islak imzalı sözleşme belgesi yüklendiğinde tetiklenir.
/// </summary>
public sealed record SignedContractDocumentUploaded(
    Guid ContractId,
    string ObjectPath,
    string UploadedBy,
    DateTime UploadedAt);
