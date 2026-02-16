namespace MESNET.Business.Application.Commands;

public sealed record RejectBusiness(
    Guid BusinessId,
    string Reason);
