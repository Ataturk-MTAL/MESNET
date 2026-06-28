namespace MESNET.Institution.Shared.Events;

public sealed record StaffAuthorized(Guid InstitutionId, Guid StaffMemberId, string Role, string? BranchCode);
