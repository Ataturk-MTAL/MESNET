using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Dtos;
using MESNET.Payment.Application.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Payment.Api;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/payments").RequireAuthorization();

        group.MapGet("/{id:guid}", Get).RequireAuthorization(Permissions.Salary.View);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Salary.View);
        group.MapPost("/{id:guid}/upload-receipt/business", PostUploadReceiptByBusiness).RequireAuthorization(Permissions.Company.UploadReceipt);
        group.MapPost("/{id:guid}/upload-receipt/student", PostUploadReceiptByStudent).RequireAuthorization(Permissions.Salary.Receipt);
        group.MapPost("/{id:guid}/confirm", PostConfirm).RequireAuthorization(Permissions.Salary.Receipt);
        group.MapPost("/{id:guid}/approve/teacher", PostApproveTeacher).RequireAuthorization(Permissions.Salary.Approve);
        group.MapPost("/{id:guid}/approve/deputy", PostApproveDeputy).RequireAuthorization(Permissions.Salary.Approve);
        group.MapPost("/{id:guid}/reject", PostReject).RequireAuthorization(Permissions.Salary.Approve);
        // Okuma okullara açık, YAZMA ulusal izin ister (#147) — aynı ucun iki farklı izni.
        group.MapGet("/config/minimum-wage", GetMinimumWageHistory)
            .RequireAuthorization(Permissions.Salary.ParameterView);
        group.MapPut("/config/minimum-wage", PutMinimumWage)
            .RequireAuthorization(Permissions.Platform.ParameterManage);
        group.MapPost("/open-monthly-periods", PostOpenMonthlyPeriods).RequireAuthorization(Permissions.Salary.Calculate);
    }

    private static async Task<IResult> Get(Guid id, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<PaymentSummaryDto>(new GetPaymentSummary(id));
        return Results.Ok(ResponseBuilder.Success().AddData(dto).Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId = null, Guid? businessId = null, Guid? institutionId = null,
        Guid? academicPeriodId = null,
        string? phase = null, string? month = null, string? branchCode = null,
        string? monthFrom = null, string? monthTo = null,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<PaymentSummaryDto>>(
            new ListPaymentSummaries(studentId, businessId, institutionId, academicPeriodId, phase, month, branchCode, monthFrom, monthTo)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });
        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/upload-receipt/business — İşletme dekontu yükler
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostUploadReceiptByBusiness(
        Guid id, HttpRequest request, IMessageBus bus)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("Multipart form-data bekleniyor.").Build());

        var form = await request.ReadFormAsync();

        if (!Guid.TryParse(form["StudentId"], out var studentId))
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("StudentId geçersiz veya eksik.").Build());

        if (!Guid.TryParse(form["BusinessId"], out var businessId))
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("BusinessId geçersiz veya eksik.").Build());

        if (!int.TryParse(form["Month"], out var month))
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("Month geçersiz veya eksik.").Build());

        if (!int.TryParse(form["Year"], out var year))
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("Year geçersiz veya eksik.").Build());

        var receiptFile = form.Files.GetFile("ReceiptFile");
        if (receiptFile is null)
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("ReceiptFile eksik.").Build());

        var receiptId = await bus.InvokeAsync<Guid>(new UploadReceiptByBusiness(id, studentId, businessId, month, year, receiptFile));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { receiptId })
            .AddMessage("İşletme dekontu yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/upload-receipt/student — Öğrenci dekontu yükler (fallback)
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostUploadReceiptByStudent(
        Guid id, HttpRequest request, IMessageBus bus)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("Multipart form-data bekleniyor.").Build());

        var form = await request.ReadFormAsync();

        if (!Guid.TryParse(form["StudentId"], out var studentId))
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("StudentId geçersiz veya eksik.").Build());

        if (!int.TryParse(form["Month"], out var month))
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("Month geçersiz veya eksik.").Build());

        if (!int.TryParse(form["Year"], out var year))
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("Year geçersiz veya eksik.").Build());

        var receiptFile = form.Files.GetFile("ReceiptFile");
        if (receiptFile is null)
            return Results.BadRequest(ResponseBuilder.Fail().AddMessage("ReceiptFile eksik.").Build());

        var receiptId = await bus.InvokeAsync<Guid>(new UploadReceiptByStudent(id, studentId, month, year, receiptFile));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { receiptId })
            .AddMessage("Öğrenci dekontu yüklendi.")
            .Build());
    }

    private static async Task<IResult> PostConfirm(Guid id, ConfirmSalary command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { SalaryPeriodId = id });
        return Results.Ok(ResponseBuilder.Success().AddMessage("Öğrenci maaşı aldığını onayladı.").Build());
    }

    private static async Task<IResult> PostApproveTeacher(Guid id, ApproveReceiptByTeacher command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { SalaryPeriodId = id });
        return Results.Ok(ResponseBuilder.Success().AddMessage("Koordinatör öğretmen dekontu onayladı.").Build());
    }

    private static async Task<IResult> PostApproveDeputy(Guid id, ApproveReceiptByDeputy command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { SalaryPeriodId = id });
        return Results.Ok(ResponseBuilder.Success().AddMessage("Müdür yardımcısı dekontu onayladı. Ödeme tamamlandı.").Build());
    }

    private static async Task<IResult> PostReject(Guid id, RejectReceipt command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { SalaryPeriodId = id });
        return Results.Ok(ResponseBuilder.Success().AddMessage("Dekont reddedildi.").Build());
    }

    /// <summary>
    /// Asgari ücret yürürlük geçmişi — geçmiş, yürürlükteki ve ileri tarihli dönemler.
    /// Kurum kapsamı handler'da token'dan okunur, sorgu dizesinden alınmaz.
    /// </summary>
    private static async Task<IResult> GetMinimumWageHistory(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<SalaryConfigHistoryDto>(new GetSalaryConfigHistory());
        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }

    private static async Task<IResult> PutMinimumWage(UpdateMinimumWage command, IMessageBus bus)
    {
        await bus.InvokeAsync(command);
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Asgari ücret güncellendi.")
            .Build());
    }

    /// <summary>
    /// Verilen ay için maaş dönemlerini elle açar. Normalde ay sonunda zamanlayıcı yapar;
    /// bu endpoint kaçırılmış koşu, sisteme geçişin ilk ayı veya sonradan eklenen yerleştirmeler
    /// içindir. Zaten kaydı olan öğrenciler atlanır, tekrar çalıştırmak güvenlidir (#63).
    /// </summary>
    private static async Task<IResult> PostOpenMonthlyPeriods(
        OpenMonthlyPeriodsRequest? request, IMessageBus bus)
    {
        var referenceDate = request?.ReferenceDate ?? DateTime.UtcNow;
        var month = request?.Month ?? referenceDate.ToString("yyyy-MM");

        var result = await bus.InvokeAsync<OpenMonthlySalaryPeriodsResult>(
            new OpenMonthlySalaryPeriods(month, referenceDate));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage($"{result.Opened} maaş dönemi açıldı, {result.Skipped} kayıt zaten vardı.")
            .Build());
    }
}

/// <summary>Gövde boş bırakılırsa içinde bulunulan ay ve şu an kullanılır.</summary>
public sealed record OpenMonthlyPeriodsRequest(string? Month, DateTime? ReferenceDate);
