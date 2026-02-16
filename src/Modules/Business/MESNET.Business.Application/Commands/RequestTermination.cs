namespace MESNET.Business.Application.Commands;

public sealed record RequestTermination(
    Guid BusinessId,
    Guid StudentId,
    string Reason);
