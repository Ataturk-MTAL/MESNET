using MESNET.Business.Application.Commands;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Business.Api;

/// <summary>
/// Usta öğretici belgesi yönetimi endpoint'leri.
/// </summary>
public static class BusinessInstructorDocumentEndpoints
{
    public static IEndpointRouteBuilder MapBusinessInstructorDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var docGroup = app.MapGroup("/api/businesses/{businessId:guid}/instructor-document")
            .WithTags("BusinessInstructorDocument").RequireAuthorization();
        docGroup.MapPost("/", PostUploadInstructorDocument).RequireAuthorization(Permissions.Company.MasterTrainer);
        docGroup.MapPost("/{documentId:guid}/invalidate", PostInvalidate).RequireAuthorization(Permissions.Company.MasterTrainer);
        docGroup.MapPost("/{documentId:guid}/delete", Delete).RequireAuthorization(Permissions.Company.MasterTrainer);
        docGroup.MapPost("/request", PostRequest).RequireAuthorization(Permissions.Company.MasterTrainer);

        app.MapPost("/api/businesses/{businessId:guid}/suspend", PostSuspend)
            .WithTags("Business").RequireAuthorization(Permissions.Company.Manage);

        return app;
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/businesses/{businessId}/instructor-document — Usta öğretici belgesi yükle
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostUploadInstructorDocument(
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

        var ev = await bus.InvokeAsync<InstructorDocumentUploaded>(command);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { documentId = ev.DocumentId })
            .AddMessage("Usta öğretici belgesi yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/businesses/{businessId}/instructor-document/{documentId}/invalidate — Belgeyi geçersiz kıl
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostInvalidate(
        Guid businessId, Guid documentId, InvalidateInstructorDocument command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { BusinessId = businessId, DocumentId = documentId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Usta öğretici belgesi geçersiz kılındı.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // DELETE /api/businesses/{businessId}/instructor-document/{documentId} — Belgeyi sil
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> Delete(
        Guid businessId, Guid documentId, DeleteInstructorDocument command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { BusinessId = businessId, DocumentId = documentId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Usta öğretici belgesi silindi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/businesses/{businessId}/instructor-document/request — Belge talebi gönder
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostRequest(
        Guid businessId, RequestInstructorDocument command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { BusinessId = businessId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Usta öğretici belgesi talebi gönderildi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/businesses/{businessId}/suspend — İşletmeyi pasife al
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostSuspend(
        Guid businessId, SuspendBusiness command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { BusinessId = businessId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme pasife alındı.")
            .Build());
    }
}
