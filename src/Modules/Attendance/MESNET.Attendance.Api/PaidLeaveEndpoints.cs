using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Queries;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Attendance.Api;

/// <summary>
/// MESEM ücretli izin başvurusu uçları (#177).
///
/// <para><b>Kapsam alanları claim'den okunur, istekten ALINMAZ.</b> İki taraflı onayı ayakta
/// tutan şey budur: <c>InstitutionManager</c> her domain wildcard'ını taşıdığı için işletme
/// adımının izni ona da gider, ama <c>business_id</c> claim'i yoktur (ADR-0001). Aynı desen
/// <c>StudentTermGradeEndpoints</c>'te kullanılıyor.</para>
/// </summary>
public static class PaidLeaveEndpoints
{
    public static void MapPaidLeaveEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendance/paid-leave")
            .WithTags("PaidLeave").RequireAuthorization();

        // Öğrenci başvurusu — StudentId token'dan gelir.
        group.MapPost("/", Post).RequireAuthorization(Permissions.Attendance.LeaveRequest);

        // 1. adım: işletme. İzin uca erişimi açar; adımı business_id kapsamı bağlar.
        group.MapPost("/{requestId:guid}/business-approve", PostBusinessApprove)
            .RequireAuthorization(Permissions.Attendance.LeaveBusinessApprove);
        group.MapPost("/{requestId:guid}/business-reject", PostBusinessReject)
            .RequireAuthorization(Permissions.Attendance.LeaveBusinessApprove);

        // 2. adım: okul (müdür yardımcısı/müdür). İzin bu uçla resmîleşir.
        group.MapPost("/{requestId:guid}/approve", PostApprove)
            .RequireAuthorization(Permissions.Attendance.LeaveApprove);
        group.MapPost("/{requestId:guid}/reject", PostReject)
            .RequireAuthorization(Permissions.Attendance.LeaveApprove);

        // Listeleme herkese açıktır ama KAPSAM daralttır: okul kurumu, işletme kendi
        // başvurularını, öğrenci yalnız kendisininkini görür (handler karar verir).
        group.MapGet("/", GetAll);
    }

    private static async Task<IResult> Post(
        RequestPaidLeave command, Guid academicPeriodId, IMessageBus bus, HttpContext http)
    {
        var requestId = await bus.InvokeAsync<Guid>(command with
        {
            StudentId = ClaimGuid(http, "student_id"),
            AcademicPeriodId = academicPeriodId
        });

        return Results.Created(
            $"/api/attendance/paid-leave/{requestId}",
            ResponseBuilder.Success(201)
                .AddData(new { requestId })
                .AddMessage("Ücretli izin başvurusu alındı. İşletme ve okul onayı bekleniyor.")
                .Build());
    }

    private static async Task<IResult> PostBusinessApprove(
        Guid requestId, IMessageBus bus, HttpContext http)
    {
        await bus.InvokeAsync(new BusinessApprovePaidLeave(requestId)
        {
            BusinessIdClaim = ClaimGuid(http, "business_id")
        });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Başvuru işletme adına onaylandı. Okul onayı bekleniyor.")
            .Build());
    }

    private static async Task<IResult> PostBusinessReject(
        Guid requestId, RejectPaidLeave command, IMessageBus bus, HttpContext http)
    {
        await bus.InvokeAsync(command with
        {
            RequestId = requestId,
            BusinessIdClaim = ClaimGuid(http, "business_id")
        });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Başvuru reddedildi.")
            .Build());
    }

    private static async Task<IResult> PostApprove(Guid requestId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApprovePaidLeave(requestId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Ücretli izin onaylandı. İzin günleri devamsızlık kaydına işlenecek.")
            .Build());
    }

    private static async Task<IResult> PostReject(
        Guid requestId, RejectPaidLeave command, IMessageBus bus)
    {
        // Okul adımında business_id kapsamı aranmaz; handler zaten yalnız PendingSchool
        // durumundaki başvuruyu bu yoldan reddeder.
        await bus.InvokeAsync(command with { RequestId = requestId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Başvuru reddedildi.")
            .Build());
    }

    private static async Task<IResult> GetAll(
        string? status, Guid? academicPeriodId,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false,
        IMessageBus bus = default!, HttpContext http = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<PaidLeaveRequestDto>>(
            new ListPaidLeaveRequests(status)
            {
                BusinessIdClaim = ClaimGuid(http, "business_id"),
                StudentIdClaim = ClaimGuid(http, "student_id"),
                InstitutionIdClaim = ClaimGuid(http, "institution_id"),
                AcademicPeriodId = academicPeriodId,
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                Descending = descending
            });

        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }

    /// <summary>Token claim'i — yoksa <c>Guid.Empty</c> (handler kapsam kontrolünde reddeder).</summary>
    private static Guid ClaimGuid(HttpContext http, string claimType) =>
        Guid.TryParse(http.User.FindFirst(claimType)?.Value, out var id) ? id : Guid.Empty;
}
