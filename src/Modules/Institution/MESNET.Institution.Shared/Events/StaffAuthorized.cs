using MESNET.Institution.Core.Enums;

namespace MESNET.Institution.Shared.Events;

public sealed record StaffAuthorized(Guid InstitutionId, Guid StaffMemberId, StaffRole Role, string? BranchCode);
