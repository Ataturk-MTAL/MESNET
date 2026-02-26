using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Queries;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Business.Api;

public static class BusinessDocumentEndpoints
{
    public static IEndpointRouteBuilder MapBusinessDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/businesses/{businessId:guid}/documents")
            .WithTags("BusinessDocument")
            .RequireAuthorization();

        group.MapPost("/", Post).DisableAntiforgery().RequireAuthorization(Permissions.Company.Document);
        group.MapPost("/{documentId:guid}/approve", PostApprove).RequireAuthorization(Permissions.Document.Approve);
        group.MapGet("/{documentId:guid}/url", GetDownloadUrl).RequireAuthorization(Permissions.Company.Document);
        group.MapDelete("/{documentId:guid}", Delete).RequireAuthorization(Permissions.Company.Document);

        return app;
    }

    private static async Task<IResult> Post(
        Guid businessId,
        IFormFile documentFile,
        [FromForm] string type,
        IMessageBus bus)
    {
        var docType = DocumentType.TryFromName(type, out var parsed)
            ? parsed
            : throw new DomainException("Business.InvalidDocumentType", $"Geçersiz belge tipi: {type}");

        var command = new UploadDocument(businessId, docType, documentFile, null);
        var uploaded = await bus.InvokeAsync<BusinessDocumentUploaded>(command);

        return Results.Created(
            $"/api/businesses/{businessId}/documents/{uploaded.DocumentId}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId = uploaded.DocumentId })
                .AddMessage("Belge yüklendi.")
                .Build());
    }

    private static async Task<IResult> PostApprove(Guid businessId, Guid documentId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveDocument(businessId, documentId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Belge onaylandı.")
            .Build());
    }

    private static async Task<IResult> GetDownloadUrl(Guid businessId, Guid documentId, IMessageBus bus)
    {
        var url = await bus.InvokeAsync<string>(new GetDocumentDownloadUrl(businessId, documentId));
        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { url })
            .AddMessage("İndirme bağlantısı oluşturuldu.")
            .Build());
    }

    private static async Task<IResult> Delete(Guid businessId, Guid documentId, IMessageBus bus)
    {
        await bus.InvokeAsync(new DeleteBusinessDocument(businessId, documentId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Belge silindi.")
            .Build());
    }
}
