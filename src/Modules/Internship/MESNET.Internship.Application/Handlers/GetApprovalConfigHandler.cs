using Marten;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Queries;
using MESNET.Internship.Core.Entities;

namespace MESNET.Internship.Application.Handlers;

/// <summary>
/// Eşiği okur. <b>Belge yoksa varsayılan döner ve belge YAZILMAZ</b> — okuma ucunun yan etkisi
/// olmaz.
/// </summary>
public static class GetApprovalConfigHandler
{
    public static async Task<ApprovalConfigDto> Handle(
        GetApprovalConfig query, IQuerySession session, CancellationToken cancellationToken)
    {
        var config = await session.LoadAsync<InternshipApprovalConfig>(
            InternshipApprovalConfig.SingletonId, cancellationToken);

        return new ApprovalConfigDto(
            config?.StuckApprovalDays ?? InternshipApprovalConfig.DefaultStuckApprovalDays);
    }
}
