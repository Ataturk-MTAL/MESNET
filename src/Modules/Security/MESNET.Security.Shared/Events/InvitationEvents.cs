namespace MESNET.Security.Shared.Events;

public sealed record InvitationCreated(
    Guid InvitationId,
    string Email,
    string FullName,
    string TargetRole,
    Guid? InstitutionId,
    Guid? BusinessId);

/// <param name="ApprovedById">
/// Onaylayan kullanıcının kimliği — token'dan gelir, istekten ALINMAZ (#137).
/// Modüller arası olayda ad taşınmaz; tüketen modül adı kendi <c>UserNameView</c>'ından çözer.
/// </param>
public sealed record InvitationApproved(
    Guid InvitationId,
    string Email,
    string FullName,
    string TargetRole,
    Guid ApprovedById);

/// <param name="RejectedById">Bkz. <see cref="InvitationApproved.ApprovedById"/>.</param>
public sealed record InvitationRejected(
    Guid InvitationId,
    string Email,
    string TargetRole,
    Guid RejectedById,
    string Reason);

public sealed record InvitationCompleted(
    Guid InvitationId,
    Guid UserAccountId,
    string KeycloakUserId,
    string FullName,
    string TargetRole);
