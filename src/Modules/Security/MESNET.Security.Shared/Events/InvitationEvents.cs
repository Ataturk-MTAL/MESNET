namespace MESNET.Security.Shared.Events;

public sealed record InvitationCreated(
    Guid InvitationId,
    string Email,
    string FullName,
    string TargetRole,
    Guid? InstitutionId,
    Guid? BusinessId);

public sealed record InvitationApproved(
    Guid InvitationId,
    string Email,
    string FullName,
    string TargetRole,
    string ApprovedByName);

public sealed record InvitationRejected(
    Guid InvitationId,
    string Email,
    string TargetRole,
    string RejectedByName,
    string Reason);

public sealed record InvitationCompleted(
    Guid InvitationId,
    Guid UserAccountId,
    string KeycloakUserId,
    string FullName,
    string TargetRole);
