using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Application.Handlers;
using MESNET.Reporting.Core.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Reporting.Api;

public static class DocumentLifecycleEndpoints
{
    public static void MapDocumentLifecycleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports/documents").WithTags("DocumentLifecycle");

        group.MapGet("/{documentId:guid}", GetDocument);
        group.MapGet("/{documentId:guid}/pdf", GetDocumentPdf);
        group.MapGet("/", GetDocuments);
        group.MapGet("/by-student/{studentId:guid}", GetDocumentsByStudent);
        group.MapPost("/{documentId:guid}/print", MarkAsPrinted);
        group.MapPost("/{documentId:guid}/sign-and-return", MarkAsSignedAndReturned);
        group.MapPost("/{documentId:guid}/archive", MarkAsArchived);
        group.MapDelete("/{documentId:guid}", DeleteDocument);
        group.MapPost("/batch-delete", DeleteDocumentsBatch);
    }

    // --- Dokuman detayi (FormDataJson haric) ---
    private static async Task<IResult> GetDocument(Guid documentId, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<GeneratedDocumentSummary>>(new GetDocumentById(documentId));

        if (result.IsFailure)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result.Value)
            .Build());
    }

    // --- PDF indir (presigned URL veya on-demand fallback) ---
    private static async Task<IResult> GetDocumentPdf(Guid documentId, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<PdfDownloadResult>>(new GetDocumentPdf(documentId));

        if (result.IsFailure)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        var download = result.Value;

        // MinIO presigned URL varsa redirect
        if (download.HasPresignedUrl)
            return Results.Ok(ResponseBuilder.Success()
                .AddData(new { url = download.PresignedUrl })
                .AddMessage("PDF indirme bağlantısı oluşturuldu.")
                .Build());

        // On-demand fallback: dogrudan PDF stream dondur
        return Results.File(download.PdfBytes!, "application/pdf", $"document-{documentId}.pdf");
    }

    // --- Filtreli dokuman listesi ---
    private static async Task<IResult> GetDocuments(
        string? status, string? formType, Guid? teacherId, Guid? institutionId,
        IMessageBus bus)
    {
        var query = new GetPendingDocuments(status, formType, teacherId, institutionId);
        var documents = await bus.InvokeAsync<IReadOnlyList<GeneratedDocumentSummary>>(query);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(documents)
            .Build());
    }

    // --- Ogrenciye ait dokumanlar ---
    private static async Task<IResult> GetDocumentsByStudent(Guid studentId, IMessageBus bus)
    {
        var documents = await bus.InvokeAsync<IReadOnlyList<GeneratedDocumentSummary>>(
            new GetDocumentsByStudent(studentId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(documents)
            .Build());
    }

    // --- Yazdirildi olarak isaretle ---
    private static async Task<IResult> MarkAsPrinted(Guid documentId, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var result = await bus.InvokeAsync<Result>(new MarkDocumentAsPrinted(documentId, user));

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Doküman yazdırıldı olarak işaretlendi.")
            .Build());
    }

    // --- Imzalanip teslim edildi olarak isaretle ---
    private static async Task<IResult> MarkAsSignedAndReturned(Guid documentId, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var result = await bus.InvokeAsync<Result>(new MarkDocumentAsSignedAndReturned(documentId, user));

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Doküman imzalanıp teslim edildi olarak işaretlendi.")
            .Build());
    }

    // --- Arsivle ---
    private static async Task<IResult> MarkAsArchived(Guid documentId, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var result = await bus.InvokeAsync<Result>(new MarkDocumentAsArchived(documentId, user));

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Doküman arşivlendi.")
            .Build());
    }

    // --- Tekil silme ---
    private static async Task<IResult> DeleteDocument(Guid documentId, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var result = await bus.InvokeAsync<Result>(new DeleteDocument(documentId, user));

        if (result.IsFailure)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Doküman silindi.")
            .Build());
    }

    // --- Toplu silme (secili ID'ler -- geri alinamaz, frontend onay dialogu zorunlu) ---
    private static async Task<IResult> DeleteDocumentsBatch(DeleteDocumentsBatchRequest request, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        var result = await bus.InvokeAsync<Result>(new DeleteDocumentsBatch(request.DocumentIds, user));

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage($"{request.DocumentIds.Count} doküman silindi.")
            .Build());
    }

    private static UserContext ExtractUserContext(HttpContext http)
    {
        var userId = Guid.TryParse(http.User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;
        var fullName = http.User.FindFirst("name")?.Value
                       ?? http.User.FindFirst("preferred_username")?.Value
                       ?? "Bilinmeyen Kullanıcı";
        return new UserContext(userId, fullName);
    }
}

/// <summary>
/// Toplu silme istegi body'si
/// </summary>
public sealed record DeleteDocumentsBatchRequest(List<Guid> DocumentIds);
