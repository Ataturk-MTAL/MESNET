using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessActivated(
    Guid BusinessId,
    Guid InstitutionId,
    string Name,
    string? Address,
    Location? Location);
