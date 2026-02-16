namespace MESNET.Business.Application.Commands;

public sealed record DeactivateBusiness(
    Guid BusinessId,
    string Reason);
