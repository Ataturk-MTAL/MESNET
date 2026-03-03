using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessApproved(
    Guid BusinessId,
    Guid TenantId,
    string Name,
    string? Address,
    Location? Location,
    string ApprovedBy,
    DateTime ApprovedAt);
