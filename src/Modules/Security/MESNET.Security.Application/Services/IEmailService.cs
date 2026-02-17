using MESNET.Common.Shared;

namespace MESNET.Security.Application.Services;

public interface IEmailService
{
    Task<Result> SendInvitationEmailAsync(
        string toEmail, string fullName, string targetRole,
        Guid invitationId, CancellationToken ct = default);
}
