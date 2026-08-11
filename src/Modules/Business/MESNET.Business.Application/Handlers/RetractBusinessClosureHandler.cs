using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.Services;
using MESNET.Business.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Microsoft.Extensions.Configuration;

namespace MESNET.Business.Application.Handlers;

/// <summary>
/// Okul kendi kapatma bildirimini geri çeker (#151).
///
/// <para>Yeter sayı bildirimlerden <b>hesaplandığı</b> için geri çekme sayıyı düşürür; eşiğin
/// altına inerse işletme <b>kendiliğinden</b> açılır. Durum alanı bildirimden bağımsız
/// tutulsaydı bu otomatik geri dönüş mümkün olmazdı.</para>
///
/// <para><b>Yalnız kendi bildirimi.</b> Bir okul başka okulun bildirimini kaldırabilseydi yeter
/// sayı anlamsızlaşırdı: iki okulun kararını üçüncü okul tek başına bozardı.</para>
/// </summary>
public static class RetractBusinessClosureHandler
{
    private const string QuorumKey = "Business:ClosureQuorum";

    public static async Task<BusinessClosureRetracted> Handle(
        RetractBusinessClosure command, IDocumentSession session,
        ICurrentUserService currentUser, IConfiguration configuration)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        var institutionId = currentUser.GetCurrentUser()?.InstitutionId;
        if (institutionId is not { } actorInstitution || actorInstitution == Guid.Empty)
            throw new DomainException(BusinessErrors.InstitutionScopeMissing());

        var own = business.ClosureReports
            .FirstOrDefault(r => BusinessClosurePolicy.CanRetract(r, actorInstitution));

        if (own is null)
            throw new DomainException(BusinessErrors.ClosureReportNotFound(command.BusinessId));

        business.ClosureReports.Remove(own);

        var quorum = configuration.GetValue<int?>(QuorumKey) ?? BusinessClosurePolicy.DefaultQuorum;
        var reportingCount = BusinessClosurePolicy.DistinctReportingInstitutions(business.ClosureReports);

        // Eşiğin altına inildiyse kapalı işletme kendiliğinden açılır. Durum zaten kapalı
        // değilse dokunulmaz — bildirim geri çekmek başka bir sebeple pasif olan işletmeyi
        // aktifleştirmemeli.
        // Geçiş kuralına UYULUR — açık kodlanmaz. İlk sürümde durum doğrudan atanıyordu ve
        // aynı geçiş açma ucundan 422 dönüyordu; iki yol aynı kurala bakmalı.
        var reopened = business.Status == BusinessStatus.Closed
            && business.Status.CanTransitionTo(BusinessStatus.Active)
            && !BusinessClosurePolicy.ReachesQuorum(business.ClosureReports, quorum);

        if (reopened)
        {
            business.Status = BusinessStatus.Active;
            business.ClosedAt = null;
        }

        session.Store(business);

        return new BusinessClosureRetracted(
            business.Id, actorInstitution, reportingCount, reopened);
    }
}
