using Marten;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Helpers;
using MESNET.Attendance.Application.Queries;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.Services;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;

namespace MESNET.Attendance.Application.Handlers;

/// <summary>
/// Ücretli izin başvurularını listeler (#177).
///
/// <para><b>Kapsam kararı permission + claim ile verilir, rol adına bakılmaz</b> (ADR-0001):
/// okul onayını verebilen kullanıcı kurumun tümünü görür; işletme kullanıcısı
/// <c>business_id</c> claim'iyle kendi başvurularını; öğrenci <c>student_id</c> claim'iyle
/// yalnız kendisininkini. Hiçbir kapsam çözülemezse <b>boş liste</b> döner — kapsamsız
/// kullanıcıya tüm kurumun izin geçmişini açmaktansa hiçbir şey göstermek doğrudur.</para>
/// </summary>
public static class ListPaidLeaveRequestsHandler
{
    public static async Task<PagedResult<PaidLeaveRequestDto>> Handle(
        ListPaidLeaveRequests query, IQuerySession session, ICurrentUserService currentUser)
    {
        IQueryable<PaidLeaveRequest> queryable = session.Query<PaidLeaveRequest>();

        var scoped = ApplyScope(queryable, query, currentUser);
        if (scoped is null)
            return EmptyPage(query);

        queryable = scoped;

        if (query.AcademicPeriodId is { } periodId)
            queryable = queryable.Where(r => r.AcademicPeriodId == periodId);

        if (!string.IsNullOrWhiteSpace(query.Status)
            && PaidLeaveStatus.TryFromName(query.Status, true, out var status))
            queryable = queryable.Where(r => r.StatusName == status.Name);

        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: r => r.StartDate);

        var page = await queryable.ToPagedResultAsync(query, r => r);

        // Aktör adları saklanmaz, okuma anında çözülür (#139).
        var names = await UserNameResolver.ResolveAsync(session, page.Items.SelectMany(ActorIds));

        return new PagedResult<PaidLeaveRequestDto>
        {
            Items = [.. page.Items.Select(r => ToDto(r, names))],
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }

    /// <summary>
    /// Kapsam filtresini uygular. Çözülemeyen kapsam için <c>null</c> döner (boş sonuç).
    /// </summary>
    private static IQueryable<PaidLeaveRequest>? ApplyScope(
        IQueryable<PaidLeaveRequest> queryable,
        ListPaidLeaveRequests query,
        ICurrentUserService currentUser)
    {
        // Okul tarafı: onay yetkisi olan kullanıcı kendi kurumunun tüm başvurularını görür.
        if (currentUser.HasPermission(Permissions.Attendance.LeaveApprove))
        {
            return query.InstitutionIdClaim is { } institutionId && institutionId != Guid.Empty
                ? queryable.Where(r => r.InstitutionId == institutionId)
                : null;
        }

        if (query.BusinessIdClaim is { } businessId && businessId != Guid.Empty)
            return queryable.Where(r => r.BusinessId == businessId);

        if (query.StudentIdClaim is { } studentId && studentId != Guid.Empty)
            return queryable.Where(r => r.StudentId == studentId);

        return null;
    }

    private static PagedResult<PaidLeaveRequestDto> EmptyPage(ListPaidLeaveRequests query) => new()
    {
        Items = [],
        TotalCount = 0,
        Page = query.Page,
        PageSize = query.PageSize
    };

    private static IEnumerable<Guid> ActorIds(PaidLeaveRequest request)
    {
        yield return request.RequestedById;
        if (request.BusinessApprovedById is { } businessApprover) yield return businessApprover;
        if (request.ApprovedById is { } approver) yield return approver;
        if (request.RejectedById is { } rejecter) yield return rejecter;
    }

    private static PaidLeaveRequestDto ToDto(
        PaidLeaveRequest request, IReadOnlyDictionary<Guid, string> names) => new(
        request.Id,
        request.StudentId,
        request.BusinessId,
        request.InstitutionId,
        request.AcademicPeriodId,
        request.StartDate,
        request.EndDate,
        PaidLeaveApprovalPolicy.DayCount(request.StartDate, request.EndDate),
        request.Reason,
        request.Status.Name,
        request.Status.Slug,
        request.RequestedById,
        names.NameOf(request.RequestedById),
        request.RequestedAt,
        request.BusinessApprovedById,
        names.NameOf(request.BusinessApprovedById),
        request.BusinessApprovedAt,
        request.ApprovedById,
        names.NameOf(request.ApprovedById),
        request.ApprovedAt,
        request.RejectedById,
        names.NameOf(request.RejectedById),
        request.RejectedAt,
        request.RejectionReason);
}
