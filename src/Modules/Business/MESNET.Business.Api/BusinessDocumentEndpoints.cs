using MESNET.Business.Application.Commands;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Business.Api;

public static class BusinessDocumentEndpoints
{
    public static IEndpointRouteBuilder MapBusinessDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/businesses/{businessId:guid}/documents").WithTags("BusinessDocument").RequireAuthorization();
        group.MapPost("/", Post).RequireAuthorization(Permissions.Company.Document);
        group.MapPost("/{documentId:guid}/approve", PostApprove).RequireAuthorization(Permissions.Document.Approve);
        return app;
    }

    private static async Task<IResult> Post(Guid businessId, UploadDocument command, IMessageBus bus)
    {
        var uploaded = await bus.InvokeAsync<BusinessDocumentUploaded>(command with { BusinessId = businessId });
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
}
