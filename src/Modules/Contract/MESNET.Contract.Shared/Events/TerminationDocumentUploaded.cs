namespace MESNET.Contract.Shared.Events;

/// <summary>
/// Islak imzalı fesih belgesi yüklendiğinde tetiklenir.
/// </summary>
public sealed record TerminationDocumentUploaded(
    Guid ContractId,
    string ObjectPath,
    string UploadedBy,
    DateTime UploadedAt);
