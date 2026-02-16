namespace MESNET.Business.Shared.Events;

public sealed record BusinessDocumentApproved(
    Guid BusinessId,
    Guid DocumentId);
