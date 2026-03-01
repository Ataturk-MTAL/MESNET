using MESNET.Common.Shared.Pagination;

namespace MESNET.Security.Application.Commands;

public sealed record CreateInvitation(
    string Email,
    string FirstName,
    string LastName,
    string TargetRole,
    string CreatedByName,
    Guid? InstitutionId = null,
    Guid? BusinessId = null,
    Dictionary<string, string>? Metadata = null);

public sealed record ApproveInvitation(Guid InvitationId, string ApprovedByName);

public sealed record RejectInvitation(Guid InvitationId, string RejectedByName, string Reason);

public sealed record CompleteInvitation(Guid InvitationId, string Username, string Password);

public sealed record GetInvitations(
    Guid? InstitutionId = null,
    string? Status = null,
    string? TargetRole = null) : PagedQuery;

public sealed record ResendInvitation(Guid InvitationId, string RequestedByName);
