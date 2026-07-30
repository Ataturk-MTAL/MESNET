using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessApproved(
    Guid BusinessId,
    Guid InstitutionId,
    string Name,
    string? Address,
    Location? Location,
    string ApprovedBy,
    DateTime ApprovedAt);
