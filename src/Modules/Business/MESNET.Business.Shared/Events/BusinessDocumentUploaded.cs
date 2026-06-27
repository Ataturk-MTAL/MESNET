namespace MESNET.Business.Shared.Events;

public sealed record BusinessDocumentUploaded(
    Guid BusinessId,
    Guid DocumentId,
    // Modüller arası event: SmartEnum yerine Name string'i taşınır (DocumentType.Name)
    string DocumentType);
