using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
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
        var group = app.MapGroup("/api/reports/documents").WithTags("DocumentLifecycle").RequireAuthorization();

        group.MapGet("/{documentId:guid}", GetDocument).RequireAuthorization(Permissions.Document.View);
        group.MapGet("/{documentId:guid}/pdf", GetDocumentPdf).RequireAuthorization(Permissions.Document.View);
        group.MapGet("/", GetDocuments).RequireAuthorization(Permissions.Document.View);
        group.MapGet("/by-student/{studentId:guid}", GetDocumentsByStudent).RequireAuthorization(Permissions.Document.View);
        group.MapPost("/{documentId:guid}/print", MarkAsPrinted).RequireAuthorization(Permissions.Document.Track);
        group.MapPost("/{documentId:guid}/sign-and-return", MarkAsSignedAndReturned).RequireAuthorization(Permissions.Document.Verify);
        group.MapPost("/{documentId:guid}/archive", MarkAsArchived).RequireAuthorization(Permissions.Document.Approve);
        group.MapPost("/download-zip", DownloadZip).RequireAuthorization(Permissions.Document.View);
        group.MapDelete("/{documentId:guid}", DeleteDocument).RequireAuthorization(Permissions.Institution.Manage);
        group.MapPost("/batch-delete", DeleteDocumentsBatch).RequireAuthorization(Permissions.Institution.Manage);
    }

    // --- Toplu ZIP indirme ---
    private static async Task<IResult> DownloadZip(DownloadDocumentsZipRequest request, IMessageBus bus)
    {
        var zipBytes = await bus.InvokeAsync<byte[]>(
            new DownloadDocumentsZip(request.DocumentIds));

        return Results.File(zipBytes, "application/zip", "evraklar.zip");
    }

    // --- Dokuman detayi (FormDataJson haric) ---
    private static async Task<IResult> GetDocument(Guid documentId, IMessageBus bus)
    {
        var summary = await bus.InvokeAsync<GeneratedDocumentSummary>(new GetDocumentById(documentId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(summary)
            .Build());
    }

    // --- PDF indir (presigned URL veya on-demand fallback) ---
    private static async Task<IResult> GetDocumentPdf(Guid documentId, IMessageBus bus)
    {
        var download = await bus.InvokeAsync<PdfDownloadResult>(new GetDocumentPdf(documentId));

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
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<GeneratedDocumentSummary>>(
            new GetPendingDocuments(status, formType, teacherId, institutionId)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
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
        await bus.InvokeAsync(new MarkDocumentAsPrinted(documentId, user));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Doküman yazdırıldı olarak işaretlendi.")
            .Build());
    }

    // --- Imzalanip teslim edildi olarak isaretle ---
    private static async Task<IResult> MarkAsSignedAndReturned(Guid documentId, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        await bus.InvokeAsync(new MarkDocumentAsSignedAndReturned(documentId, user));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Doküman imzalanıp teslim edildi olarak işaretlendi.")
            .Build());
    }

    // --- Arsivle ---
    private static async Task<IResult> MarkAsArchived(Guid documentId, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        await bus.InvokeAsync(new MarkDocumentAsArchived(documentId, user));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Doküman arşivlendi.")
            .Build());
    }

    // --- Tekil silme ---
    private static async Task<IResult> DeleteDocument(Guid documentId, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        await bus.InvokeAsync(new DeleteDocument(documentId, user));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Doküman silindi.")
            .Build());
    }

    // --- Toplu silme (secili ID'ler -- geri alinamaz, frontend onay dialogu zorunlu) ---
    private static async Task<IResult> DeleteDocumentsBatch(DeleteDocumentsBatchRequest request, IMessageBus bus, HttpContext http)
    {
        var user = ExtractUserContext(http);
        await bus.InvokeAsync(new DeleteDocumentsBatch(request.DocumentIds, user));

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

/// <summary>
/// Toplu ZIP indirme istegi body'si
/// </summary>
public sealed record DownloadDocumentsZipRequest(List<Guid> DocumentIds);
