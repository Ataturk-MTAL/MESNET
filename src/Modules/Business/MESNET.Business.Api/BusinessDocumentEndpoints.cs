using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Business.Api;

public static class BusinessDocumentEndpoints
{
    public static IEndpointRouteBuilder MapBusinessDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/businesses/{businessId:guid}/documents").WithTags("BusinessDocument");
        group.MapPost("/", Post);
        group.MapPost("/{documentId:guid}/approve", PostApprove);
        return app;
    }

    private static async Task<IResult> Post(
        Guid businessId, UploadDocument command, IDocumentSession session, IMessageBus bus)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        var document = new BusinessDocument
        {
            Type = command.Type,
            FileName = command.FileName,
            StoragePath = command.StoragePath,
            ExpiresAt = command.ExpiresAt
        };

        business.Documents.Add(document);

        session.Store(business);

        await bus.PublishAsync(new BusinessDocumentUploaded(business.Id, document.Id, document.Type));

        return Results.Created(
            $"/api/businesses/{businessId}/documents/{document.Id}",
            ResponseBuilder.Success(201)
                .AddData(new { documentId = document.Id })
                .AddMessage("Belge yüklendi.")
                .Build());
    }

    private static async Task<IResult> PostApprove(
        Guid businessId, Guid documentId, IDocumentSession session, IMessageBus bus)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        var document = business.Documents.FirstOrDefault(d => d.Id == documentId);
        if (document is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.DocumentNotFound(documentId).Description)
                .AddErrors(BusinessErrors.DocumentNotFound(documentId))
                .Build());

        document.Status = DocumentStatus.Approved;
        document.ApprovedAt = DateTime.UtcNow;

        session.Store(business);

        await bus.PublishAsync(new BusinessDocumentApproved(business.Id, document.Id));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Belge onaylandı.")
            .Build());
    }
}
