using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.Services;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Microsoft.Extensions.Configuration;
using Wolverine;

namespace MESNET.Business.Application.Handlers;

/// <summary>
/// Kapatma <b>bildirimi</b> (#151). Tek okulun bildirimi işletmeyi kapatmaz; işletme ancak
/// <b>farklı okullardan</b> gelen bildirim sayısı yeter sayıya ulaşınca küresel olarak kapanır.
///
/// <para><b>Faz 1 davranışı değişmez:</b> varsayılan yeter sayı 1'dir, yani tek bildirim hâlâ
/// kapatır. Çok okullu kuruluma geçerken yapılandırmadan 2'ye çekilir — kod değişmez.</para>
/// </summary>
public static class CloseBusinessHandler
{
    /// <summary>Yeter sayı yapılandırma anahtarı. Yoksa <c>BusinessClosurePolicy.DefaultQuorum</c>.</summary>
    private const string QuorumKey = "Business:ClosureQuorum";

    public static async Task<object> Handle(
        CloseBusiness command, IDocumentSession session,
        ICurrentUserService currentUser, IConfiguration configuration)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        // Bildirim OKULA aittir; kapsam istekten değil claim'den okunur (ADR-0003).
        var institutionId = currentUser.GetCurrentUser()?.InstitutionId;
        if (institutionId is not { } reportingInstitution || reportingInstitution == Guid.Empty)
            throw new DomainException(BusinessErrors.InstitutionScopeMissing());

        if (business.ClosureReports.Any(r => r.InstitutionId == reportingInstitution))
            throw new DomainException(BusinessErrors.ClosureAlreadyReported(command.BusinessId));

        // Kapasite/öğretmen kontrolleri bildirim anında yapılır: kendi öğrencisi hâlâ orada olan
        // bir okulun "kapandı" demesi tutarsızdır ve yeter sayıyı gürültüyle doldurur.
        if (business.Capacity.OccupiedSlots > 0)
            throw new DomainException(BusinessErrors.HasActiveStudents(command.BusinessId));

        if (business.HasAssignedTeacher)
            throw new DomainException(BusinessErrors.HasAssignedTeacher(command.BusinessId));

        business.ClosureReports.Add(new BusinessClosureReport
        {
            InstitutionId = reportingInstitution,
            ReportedById = currentUser.GetUserId(),
            ReportedByName = currentUser.GetFullName(),
            Reason = command.Reason,
        });

        var quorum = configuration.GetValue<int?>(QuorumKey) ?? BusinessClosurePolicy.DefaultQuorum;
        var reportingCount = BusinessClosurePolicy.DistinctReportingInstitutions(business.ClosureReports);

        if (!BusinessClosurePolicy.ReachesQuorum(business.ClosureReports, quorum))
        {
            session.Store(business);
            return new BusinessClosureReported(
                business.Id, reportingInstitution, currentUser.GetUserId(),
                command.Reason, reportingCount);
        }

        // Yeter sayıya ulaşıldı — küresel kapatma. Durum geçişi ancak burada denetlenir:
        // bildirim biriktirmek durum değiştirmediği için geçiş kuralına tabi değildir.
        if (!business.Status.CanTransitionTo(BusinessStatus.Closed))
            throw new DomainException(
                BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Closed.Slug));

        business.Status = BusinessStatus.Closed;
        business.ClosedAt = DateTime.UtcNow;
        session.Store(business);

        return new BusinessClosed(business.Id, business.ClosedAt.Value);
    }
}
