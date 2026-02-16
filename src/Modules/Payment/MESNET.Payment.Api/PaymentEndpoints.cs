using Marten;
using MESNET.Common.Shared;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Errors;
using MESNET.Payment.Application.Extensions;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.Enums;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace MESNET.Payment.Api;

public static class PaymentEndpoints
{
    // ────────────────────────────────────────────────────────────────────────────────
    // GET /api/payments/{id} — PaymentSummary getir
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverineGet("/api/payments/{id}")]
    public static async Task<IResult> Get(Guid id, IDocumentSession session)
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
    [WolverineGet("/api/payments")]
    public static async Task<IResult> GetAll(
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
    [WolverinePost("/api/payments/{id}/upload-receipt/business")]
    public static async Task<IResult> PostUploadReceiptByBusiness(
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

        var result = await bus.InvokeAsync<Result<MESNET.Payment.Shared.Events.ReceiptUploadedByBusiness>>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { receiptId = result.Value.ReceiptId })
            .AddMessage("İşletme dekontu yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/upload-receipt/student — Öğrenci dekontu yükler (fallback)
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/payments/{id}/upload-receipt/student")]
    public static async Task<IResult> PostUploadReceiptByStudent(
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

        var result = await bus.InvokeAsync<Result<MESNET.Payment.Shared.Events.ReceiptUploadedByStudent>>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { receiptId = result.Value.ReceiptId })
            .AddMessage("Öğrenci dekontu yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/confirm — Öğrenci "aldım" onayı (1. adım)
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/payments/{id}/confirm")]
    public static async Task<IResult> PostConfirm(
        Guid id, ConfirmSalary command, IMessageBus bus)
    {
        try
        {
            await bus.InvokeAsync(command with { SalaryPeriodId = id });
            return Results.Ok(ResponseBuilder.Success()
                .AddMessage("Öğrenci maaşı aldığını onayladı.")
                .Build());
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(ex.Message)
                .AddErrors(PaymentErrors.OperationFailed("ConfirmSalary", ex.Message))
                .Build());
        }
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/approve/teacher — Koordinatör öğretmen onayı (2. adım)
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/payments/{id}/approve/teacher")]
    public static async Task<IResult> PostApproveTeacher(
        Guid id, ApproveReceiptByTeacher command, IMessageBus bus)
    {
        try
        {
            await bus.InvokeAsync(command with { SalaryPeriodId = id });
            return Results.Ok(ResponseBuilder.Success()
                .AddMessage("Koordinatör öğretmen dekontu onayladı.")
                .Build());
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(ex.Message)
                .AddErrors(PaymentErrors.ApprovalRequired("Öğrenci onayı"))
                .Build());
        }
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/approve/deputy — Müdür yardımcısı onayı (3. adım — final)
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/payments/{id}/approve/deputy")]
    public static async Task<IResult> PostApproveDeputy(
        Guid id, ApproveReceiptByDeputy command, IMessageBus bus)
    {
        try
        {
            await bus.InvokeAsync(command with { SalaryPeriodId = id });
            return Results.Ok(ResponseBuilder.Success()
                .AddMessage("Müdür yardımcısı dekontu onayladı. Ödeme tamamlandı.")
                .Build());
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(ex.Message)
                .AddErrors(PaymentErrors.ApprovalRequired("Öğrenci ve öğretmen onayı"))
                .Build());
        }
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/payments/{id}/reject — Dekont reddet
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/payments/{id}/reject")]
    public static async Task<IResult> PostReject(
        Guid id, RejectReceipt command, IMessageBus bus)
    {
        try
        {
            await bus.InvokeAsync(command with { SalaryPeriodId = id });
            return Results.Ok(ResponseBuilder.Success()
                .AddMessage("Dekont reddedildi.")
                .Build());
        }
        catch (Exception ex)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(PaymentErrors.OperationFailed("RejectReceipt", ex.Message).Description)
                .AddErrors(PaymentErrors.OperationFailed("RejectReceipt", ex.Message))
                .Build());
        }
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // PUT /api/payments/config/minimum-wage — Asgari ücreti güncelle (admin)
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePut("/api/payments/config/minimum-wage")]
    public static async Task<IResult> PutMinimumWage(
        UpdateMinimumWage command, IDocumentSession session)
    {
        try
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
        catch (Exception ex)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(PaymentErrors.OperationFailed("UpdateMinimumWage", ex.Message).Description)
                .AddErrors(PaymentErrors.OperationFailed("UpdateMinimumWage", ex.Message))
                .Build());
        }
    }
}
