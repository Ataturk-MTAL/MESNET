using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Errors;
using MESNET.Internship.Application.Extensions;
using MESNET.Internship.Application.Queries;
using MESNET.Internship.Core.Entities;

namespace MESNET.Internship.Application.Handlers;

/// <summary>
/// Tek bir stajın özetini döndürür.
///
/// <para><b>Kapsam burada uygulanır (#191).</b> Uç hem okul tarafına hem veri sahibine açık
/// (<c>InternshipViewOrOwn</c>) ama handler kapsamı hiç kontrol etmiyordu: kimliğini bilen
/// herhangi bir <c>view-own</c> sahibi <b>herhangi bir stajı</b> okuyabiliyordu. Liste ucu
/// kapsanmışken (#182) tekil uç atlanmıştı.</para>
///
/// <para>Boşluk #191'de büyüyecekti: işletme yetkilisine <c>internship:view-own</c> verilmesi
/// gerekiyordu ve o izin, kapsamsız tekil ucu yeni bir kitleye açardı. Bu yüzden önce burası
/// kapatıldı.</para>
/// </summary>
public static class GetInternshipHandler
{
    public static async Task<InternshipSummaryDto> Handle(
        GetInternship query, IQuerySession session, ICurrentUserService currentUser)
    {
        var summary = await session.LoadAsync<InternshipSummary>(query.InternshipId);
        if (summary is null)
            throw new DomainException(InternshipErrors.NotFound(query.InternshipId));

        EnsureInScope(currentUser, summary, query.InternshipId);

        return summary.ToDto();
    }

    /// <summary>
    /// Geniş görüntüleme izni yoksa çağıran yalnız kendi kapsamındaki stajı okuyabilir.
    /// Kapsam dışı istek <b>"bulunamadı"</b> döner — "yetkin yok" deseydik kimliğin var
    /// olduğunu doğrulamış olurduk.
    /// </summary>
    private static void EnsureInScope(
        ICurrentUserService currentUser, InternshipSummary summary, Guid internshipId)
    {
        var scope = OwnDataScope.Resolve(
            currentUser, Permissions.Internship.View, includeBusinessScope: true);

        if (scope.IsUnrestricted) return;

        // Kapsamlar birleşir — öğrenci bağı YA DA işletme eşleşmesi yeter.
        if (scope.StudentIds.Contains(summary.StudentId)) return;
        if (scope.BusinessId is { } b && summary.BusinessId == b) return;

        throw new DomainException(InternshipErrors.NotFound(internshipId));
    }
}
