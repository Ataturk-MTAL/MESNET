namespace MESNET.Business.Application.Commands;

public sealed record DeleteBusinessDocument(Guid BusinessId, Guid DocumentId);
