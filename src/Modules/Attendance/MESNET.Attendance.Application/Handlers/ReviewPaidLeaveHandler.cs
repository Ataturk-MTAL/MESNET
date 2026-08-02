using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Core.Services;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

/// <summary>
/// Ücretli izin zincirinin 1. adımı: <b>işletme onayı</b> (#177).
///
/// <para>Uç <c>attendance:leave:business-approve</c> ister, ama izin tek başına yetmez:
/// <c>InstitutionManager</c> <c>attendance:*</c> wildcard'ını taşıdığı için bu izin okul
/// müdürüne de gider. Adımı işletmeye bağlayan şey <b>kapsam</b>tır — token'daki
/// <c>business_id</c> claim'i başvurunun işletmesiyle eşleşmek zorundadır ve okul rollerinde
/// o claim yoktur (ADR-0001: permission erişimi açar, kapsamı belirlemez).</para>
/// </summary>
public static class BusinessApprovePaidLeaveHandler
{
    [AggregateHandler]
    public static async Task<PaidLeaveBusinessApproved> Handle(
        BusinessApprovePaidLeave command, PaidLeaveRequest? request,
        ICurrentUserService currentUser, IQuerySession session)
    {
        var target = EnsureExists(command.RequestId, request);

        if (!target.Status.CanBusinessApprove)
            throw new DomainException(AttendanceErrors.PaidLeaveInvalidStage(target.Status.Slug));

        if (!PaidLeaveApprovalPolicy.CanBusinessApprove(command.BusinessIdClaim, target.BusinessId))
            throw new DomainException(AttendanceErrors.PaidLeaveBusinessScopeMismatch());

        await EnsurePeriodActiveAsync(session, target);

        return new PaidLeaveBusinessApproved(
            target.Id, target.StudentId, target.BusinessId, currentUser.GetUserId(), DateTime.UtcNow);
    }

    internal static PaidLeaveRequest EnsureExists(Guid requestId, PaidLeaveRequest? request) =>
        request ?? throw new DomainException(AttendanceErrors.PaidLeaveRequestNotFound(requestId));

    /// <summary>Kapalı dönemde onay yürümez — geçmiş dönem salt okunurdur (CLAUDE.md).</summary>
    internal static async Task EnsurePeriodActiveAsync(IQuerySession session, PaidLeaveRequest request)
    {
        var period = await session.LoadAsync<AcademicPeriodView>(request.AcademicPeriodId);
        if (period is { IsActive: false })
            throw new DomainException(AttendanceErrors.AcademicPeriodClosed(request.AcademicPeriodId));
    }
}

/// <summary>
/// Ücretli izin zincirinin 2. adımı: <b>okul onayı</b> (#177) — müdür yardımcısı / müdür.
/// İzin bu komutla resmîleşir ve devamsızlık kayıtları bu olaydan doğar.
/// </summary>
public static class ApprovePaidLeaveHandler
{
    [AggregateHandler]
    public static async Task<PaidLeaveApproved> Handle(
        ApprovePaidLeave command, PaidLeaveRequest? request,
        ICurrentUserService currentUser, IQuerySession session)
    {
        var target = BusinessApprovePaidLeaveHandler.EnsureExists(command.RequestId, request);

        // Sıra sabittir: işletme onaylamadan okul onaylayamaz.
        if (!target.Status.CanSchoolApprove)
            throw new DomainException(AttendanceErrors.PaidLeaveInvalidStage(target.Status.Slug));

        var schoolApproverId = currentUser.GetUserId();

        // Tek kullanıcı iki rolü birden taşıyabilir; o hâlde "iki taraflı onay" adı kalır,
        // kendisi kalmaz. İşletme adımını yapan kullanıcı okul adımını yapamaz.
        if (!PaidLeaveApprovalPolicy.AreApproversDistinct(
                target.BusinessApprovedById ?? Guid.Empty, schoolApproverId))
            throw new DomainException(AttendanceErrors.PaidLeaveSameApprover());

        await BusinessApprovePaidLeaveHandler.EnsurePeriodActiveAsync(session, target);

        return new PaidLeaveApproved(
            target.Id,
            target.StudentId,
            target.BusinessId,
            target.InstitutionId,
            target.AcademicPeriodId,
            target.StartDate,
            target.EndDate,
            target.Reason,
            schoolApproverId,
            DateTime.UtcNow);
    }
}

/// <summary>
/// Ücretli izin başvurusunu reddeder (#177). Zincirin iki adımında da kullanılır.
///
/// <para>İşletme adımındaki ret için kapsam kontrolü onay adımıyla aynıdır: başka bir işletme
/// (ya da claim'i olmayan okul kullanıcısı) o adımı reddedemez. Okul adımındaki ret
/// <c>attendance:leave:approve</c> ile korunur.</para>
/// </summary>
public static class RejectPaidLeaveHandler
{
    [AggregateHandler]
    public static async Task<PaidLeaveRejected> Handle(
        RejectPaidLeave command, PaidLeaveRequest? request,
        ICurrentUserService currentUser, IQuerySession session)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new DomainException(AttendanceErrors.RejectionReasonRequired());

        var target = BusinessApprovePaidLeaveHandler.EnsureExists(command.RequestId, request);

        if (!target.Status.CanReject)
            throw new DomainException(AttendanceErrors.PaidLeaveInvalidStage(target.Status.Slug));

        // İşletme adımındaysa reddeden taraf da işletme olmalıdır.
        if (target.Status.CanBusinessApprove
            && !PaidLeaveApprovalPolicy.CanBusinessApprove(command.BusinessIdClaim, target.BusinessId))
            throw new DomainException(AttendanceErrors.PaidLeaveBusinessScopeMismatch());

        await BusinessApprovePaidLeaveHandler.EnsurePeriodActiveAsync(session, target);

        return new PaidLeaveRejected(
            target.Id, target.StudentId, target.BusinessId,
            currentUser.GetUserId(), DateTime.UtcNow, command.Reason.Trim(), target.Status.Name);
    }
}
