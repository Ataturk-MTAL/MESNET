using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Application.Extensions;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
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
        group.MapPut("/config/minimum-wage", PutMinimumWage).RequireAuthorization(Permissions.Salary.Parameter);
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // GET /api/payments/{id} — PaymentSummary getir
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> Get(Guid id, IDocumentSession session)
    {
        var summary = await session.LoadAsync<PaymentSummary>(id);
        if (summary is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(PaymentErrors.NotFound(id).Description)
                .AddErrors(PaymentErrors.NotFound(id))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(summary.ToDto())
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // GET /api/payments — Liste (filtreli)
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> GetAll(
        IDocumentSession session,
        Guid? studentId = null,
        Guid? businessId = null,
        Guid? institutionId = null,
        string? phase = null,
        string? month = null)
    {
        IQueryable<PaymentSummary> query = session.Query<PaymentSummary>();

        if (studentId.HasValue)
            query = query.Where(p => p.StudentId == studentId.Value);

        if (businessId.HasValue)
            query = query.Where(p => p.BusinessId == businessId.Value);

        if (institutionId.HasValue)
            query = query.Where(p => p.InstitutionId == institutionId.Value);

        if (!string.IsNullOrWhiteSpace(phase))
        {
            if (PaymentPhase.TryFromName(phase, out var phaseEnum))
                query = query.Where(p => p.Phase == phaseEnum);
        }

        if (!string.IsNullOrWhiteSpace(month))
            query = query.Where(p => p.Month == month);

        var summaries = await query.ToListAsync();
        var dtos = summaries.Select(s => s.ToDto()).ToList();

        return Results.Ok(ResponseBuilder.Success()
            .AddData(dtos)
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/upload-receipt/business — İşletme dekontu yükler
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostUploadReceiptByBusiness(
        Guid id, HttpRequest request, IMessageBus bus)
    {
        // Manuel form parsing (Wolverine IFormFile binding desteklemez)
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Multipart form-data bekleniyor.")
                .Build());
        }

        var form = await request.ReadFormAsync();

        // Parse fields
        if (!Guid.TryParse(form["StudentId"], out var studentId))
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("StudentId geçersiz veya eksik.")
                .Build());
        }

        if (!Guid.TryParse(form["BusinessId"], out var businessId))
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("BusinessId geçersiz veya eksik.")
                .Build());
        }

        if (!int.TryParse(form["Month"], out var month))
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Month geçersiz veya eksik.")
                .Build());
        }

        if (!int.TryParse(form["Year"], out var year))
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Year geçersiz veya eksik.")
                .Build());
        }

        var receiptFile = form.Files.GetFile("ReceiptFile");
        if (receiptFile is null)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("ReceiptFile eksik.")
                .Build());
        }

        var command = new UploadReceiptByBusiness(
            id,
            studentId,
            businessId,
            month,
            year,
            receiptFile);

        var result = await bus.InvokeAsync<Result<Guid>>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { receiptId = result.Value })
            .AddMessage("İşletme dekontu yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/upload-receipt/student — Öğrenci dekontu yükler (fallback)
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostUploadReceiptByStudent(
        Guid id, HttpRequest request, IMessageBus bus)
    {
        // Manuel form parsing (Wolverine IFormFile binding desteklemez)
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Multipart form-data bekleniyor.")
                .Build());
        }

        var form = await request.ReadFormAsync();

        // Parse fields
        if (!Guid.TryParse(form["StudentId"], out var studentId))
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("StudentId geçersiz veya eksik.")
                .Build());
        }

        if (!int.TryParse(form["Month"], out var month))
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Month geçersiz veya eksik.")
                .Build());
        }

        if (!int.TryParse(form["Year"], out var year))
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Year geçersiz veya eksik.")
                .Build());
        }

        var receiptFile = form.Files.GetFile("ReceiptFile");
        if (receiptFile is null)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("ReceiptFile eksik.")
                .Build());
        }

        var command = new UploadReceiptByStudent(
            id,
            studentId,
            month,
            year,
            receiptFile);

        var result = await bus.InvokeAsync<Result<Guid>>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { receiptId = result.Value })
            .AddMessage("Öğrenci dekontu yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/confirm — Öğrenci "aldım" onayı (1. adım)
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostConfirm(
        Guid id, ConfirmSalary command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(
            command with { SalaryPeriodId = id });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Öğrenci maaşı aldığını onayladı.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/approve/teacher — Koordinatör öğretmen onayı (2. adım)
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostApproveTeacher(
        Guid id, ApproveReceiptByTeacher command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(
            command with { SalaryPeriodId = id });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Koordinatör öğretmen dekontu onayladı.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/approve/deputy — Müdür yardımcısı onayı (3. adım — final)
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostApproveDeputy(
        Guid id, ApproveReceiptByDeputy command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(
            command with { SalaryPeriodId = id });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Müdür yardımcısı dekontu onayladı. Ödeme tamamlandı.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/reject — Dekont reddet
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostReject(
        Guid id, RejectReceipt command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(
            command with { SalaryPeriodId = id });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Dekont reddedildi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // PUT /api/payments/config/minimum-wage — Asgari ücreti güncelle (admin)
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PutMinimumWage(
        UpdateMinimumWage command, IDocumentSession session)
    {
        // Önceki config'i expire et
        var currentConfig = session.Query<SalaryCalculationConfig>()
            .Where(c => c.InstitutionId == command.InstitutionId)
            .Where(c => c.EffectiveTo == null)
            .FirstOrDefault();

        if (currentConfig is not null)
        {
            currentConfig.EffectiveTo = command.EffectiveFrom.AddDays(-1);
            session.Store(currentConfig);
        }

        // Yeni config oluştur
        var newConfig = new SalaryCalculationConfig
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            MinimumWage = command.NewMinimumWage,
            EffectiveFrom = command.EffectiveFrom,
            UpdatedBy = command.UpdatedBy
        };
        session.Store(newConfig);

        await session.SaveChangesAsync();

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { configId = newConfig.Id })
            .AddMessage("Asgari ücret güncellendi.")
            .Build());
    }
}
