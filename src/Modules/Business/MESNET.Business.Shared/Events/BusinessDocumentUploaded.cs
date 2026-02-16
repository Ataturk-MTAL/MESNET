using MESNET.Business.Core.Enums;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessDocumentUploaded(
    Guid BusinessId,
    Guid DocumentId,
    DocumentType DocumentType);
