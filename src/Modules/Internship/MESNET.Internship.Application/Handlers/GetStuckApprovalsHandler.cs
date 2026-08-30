using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Security;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Queries;
using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.Policies;

namespace MESNET.Internship.Application.Handlers;

/// <summary>
/// Alt ağaçtaki tıkanmış fesih onaylarını sayar.
///
/// <para><b>Kiracı sınırı bilerek aşılır.</b> <c>InternshipSaga</c> kiracıya aittir ve
/// müdürlük düğümü kiracı DEĞİLDİR — müdürlüğün kendi kiracısında sorgu boş dönerdi.
/// Marten'in <c>TenantIsOneOf(...)</c> operatörü kapsamı okul kiracılarına açar; kimlikler
/// <b>istekten değil</b> <see cref="SubtreeTenantScope"/> üzerinden aktörün
/// claim'lerinden gelir.</para>
///
/// <para><b>Boş listede sorgu HİÇ kurulmaz</b> — parametresiz <c>TenantIsOneOf()</c>'un SQL'de
/// ne ürettiğine güvenilmez.</para>
///
/// <para><b>Belgeler tam çekilir, projeksiyon yapılmaz.</b> Süzgeç zaten "eşiği aşmış açık
/// zincir" olduğu için küme küçüktür; Marten'in projeksiyon çevirisine bağımlılık eklemeye
/// değmez.</para>
/// </summary>
public static class GetStuckApprovalsHandler
{
    public static async Task<StuckApprovalSummaryDto> Handle(
        GetStuckApprovals query,
        IQuerySession session,
        ICurrentUserService currentUser,
        SubtreeTenantScope tenantScope,
        CancellationToken cancellationToken)
    {
        var config = await session.LoadAsync<InternshipApprovalConfig>(
            InternshipApprovalConfig.SingletonId, cancellationToken);
        var thresholdDays =
            config?.StuckApprovalDays ?? InternshipApprovalConfig.DefaultStuckApprovalDays;

        var visibility = InstitutionScopePolicy.VisibleScope(
            currentUser.GetCurrentUser()?.InstitutionId,
            currentUser.GetInstitutionPath(),
            currentUser.HasPermission(Permissions.Platform.TenantManage));

        var tenants = await tenantScope.ResolveAsync(visibility, cancellationToken);

        if (tenants.Count == 0)
            return new StuckApprovalSummaryDto(0, thresholdDays, []);

        var now = DateTime.UtcNow;
        var cutoff = now.AddDays(-thresholdDays);
        var tenantIds = tenants.ToArray();

        // IsCompleteOrOverridden() bir metottur ve SQL'e çevrilemez; koşul AÇILARAK yazılır.
        // Bu açılımın politikayla aynı şeyi söylediği StuckApprovalPolicyTests içindeki
        // doğruluk tablosuyla kilitlidir.
        //
        // Talep zamanı NULL olan kayıt bilerek İÇERİDE bırakılır: eksik veri sınırı
        // gevşetemez (#252).
        var stuck = await session.Query<InternshipSaga>()
            .Where(x => x.TenantIsOneOf(tenantIds)
                        && x.ApprovalChain != null
                        && !x.ApprovalChain.IsOverridden
                        && !(x.ApprovalChain.TeacherApproved
                             && x.ApprovalChain.DeputyApproved
                             && x.ApprovalChain.DirectorApproved)
                        && (x.TerminationRequestedAt == null
                            || x.TerminationRequestedAt <= cutoff))
            .ToListAsync(cancellationToken);

        var byInstitution = stuck
            .GroupBy(x => x.InstitutionId)
            .Select(g => new StuckApprovalByInstitutionDto(
                InstitutionId: g.Key,
                InstitutionName: null,
                Count: g.Count(),
                OldestDays: g
                    .Select(x => StuckApprovalPolicy.AgeInDays(x.TerminationRequestedAt, now))
                    .Where(age => age is not null)
                    .DefaultIfEmpty(null)
                    .Max()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.InstitutionId)
            .ToList();

        return new StuckApprovalSummaryDto(stuck.Count, thresholdDays, byInstitution);
    }
}
