namespace MESNET.Business.Application.Commands;

/// <summary>
/// İşletmeyi pasife al (belge ibraz edemezse).
/// </summary>
public sealed record SuspendBusiness(
    Guid BusinessId,
    string SuspendedBy,
    string Reason);
