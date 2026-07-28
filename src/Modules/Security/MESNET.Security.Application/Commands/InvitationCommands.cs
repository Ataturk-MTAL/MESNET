using MESNET.Common.Shared.Pagination;

namespace MESNET.Security.Application.Commands;

/// <remarks>
/// İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan
/// (<c>ICurrentUserService.GetUserId()</c>) damgalar. Daveti kimin oluşturduğunu/onayladığını,
/// işlemi yapan istemcinin kendisi yazamaz.
/// </remarks>
public sealed record CreateInvitation(
    string Email,
    string FirstName,
    string LastName,
    string TargetRole,
    Guid? InstitutionId = null,
    Guid? BusinessId = null,
    Dictionary<string, string>? Metadata = null);

public sealed record ApproveInvitation(Guid InvitationId);

public sealed record RejectInvitation(Guid InvitationId, string Reason);

public sealed record CompleteInvitation(Guid InvitationId, string Username, string Password);

public sealed record GetInvitations(
    Guid? InstitutionId = null,
    string? Status = null,
    string? TargetRole = null) : PagedQuery;

public sealed record ResendInvitation(Guid InvitationId);
