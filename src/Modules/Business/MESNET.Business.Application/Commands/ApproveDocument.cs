namespace MESNET.Business.Application.Commands;

public sealed record ApproveDocument(
    Guid BusinessId,
    Guid DocumentId);
