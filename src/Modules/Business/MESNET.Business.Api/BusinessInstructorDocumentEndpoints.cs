using MESNET.Business.Application.Commands;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace MESNET.Business.Api;

/// <summary>
/// Usta öğretici belgesi yönetimi endpoint'leri.
/// </summary>
public static class BusinessInstructorDocumentEndpoints
{
    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/businesses/{businessId}/instructor-document — Usta öğretici belgesi yükle
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/businesses/{businessId}/instructor-document")]
    public static async Task<IResult> PostUploadInstructorDocument(
        Guid businessId, HttpRequest request, IMessageBus bus)
    {
        // Manuel form parsing
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Multipart form-data bekleniyor.")
                .Build());
        }

        var form = await request.ReadFormAsync();

        var uploadedBy = form["UploadedBy"].ToString();
        if (string.IsNullOrWhiteSpace(uploadedBy))
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("UploadedBy geçersiz veya eksik.")
                .Build());
        }

        DateTime? expiresAt = null;
        if (DateTime.TryParse(form["ExpiresAt"], out var parsedExpiry))
        {
            expiresAt = parsedExpiry;
        }

        var documentFile = form.Files.GetFile("DocumentFile");
        if (documentFile is null)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("DocumentFile eksik.")
                .Build());
        }

        var command = new UploadInstructorDocument(
            businessId,
            documentFile,
            uploadedBy,
            expiresAt);

        var result = await bus.InvokeAsync<Result<InstructorDocumentUploaded>>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { documentId = result.Value.DocumentId })
            .AddMessage("Usta öğretici belgesi yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/businesses/{businessId}/instructor-document/{documentId}/invalidate — Belgeyi geçersiz kıl
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/businesses/{businessId}/instructor-document/{documentId}/invalidate")]
    public static async Task<IResult> PostInvalidate(
        Guid businessId, Guid documentId, InvalidateInstructorDocument command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<InstructorDocumentInvalidated>>(
            command with { BusinessId = businessId, DocumentId = documentId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Usta öğretici belgesi geçersiz kılındı.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // DELETE /api/businesses/{businessId}/instructor-document/{documentId} — Belgeyi sil
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverineDelete("/api/businesses/{businessId}/instructor-document/{documentId}")]
    public static async Task<IResult> Delete(
        Guid businessId, Guid documentId, DeleteInstructorDocument command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<InstructorDocumentDeleted>>(
            command with { BusinessId = businessId, DocumentId = documentId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Usta öğretici belgesi silindi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/businesses/{businessId}/instructor-document/request — Belge talebi gönder
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/businesses/{businessId}/instructor-document/request")]
    public static async Task<IResult> PostRequest(
        Guid businessId, RequestInstructorDocument command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<InstructorDocumentRequested>>(
            command with { BusinessId = businessId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Usta öğretici belgesi talebi gönderildi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/businesses/{businessId}/suspend — İşletmeyi pasife al
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/businesses/{businessId}/suspend")]
    public static async Task<IResult> PostSuspend(
        Guid businessId, SuspendBusiness command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<BusinessSuspended>>(
            command with { BusinessId = businessId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme pasife alındı.")
            .Build());
    }
}
