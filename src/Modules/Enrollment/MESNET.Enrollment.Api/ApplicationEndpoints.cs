using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Enrollment.Api;

public static class ApplicationEndpoints
{
    public static IEndpointRouteBuilder MapApplicationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internship-applications").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Internship.Apply);
        group.MapPost("/request", PostRequest).RequireAuthorization(Permissions.Company.RequestStudent);

        return app;
    }

    private static async Task<IResult> Post(
        ApplyForInternship command, IMessageBus bus)
    {
        var studentId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/internship-applications/{studentId}",
            ResponseBuilder.Success(201)
                .AddData(new { studentId })
                .AddMessage("Staj başvurusu oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> PostRequest(
        RequestStudent command, IMessageBus bus)
    {
        await bus.PublishAsync(
            new InternshipApplied(
                Guid.Empty,
                command.BusinessId,
                command.BranchCode,
                ApplicationSource.BusinessRequest.Name));

        return Results.Created(
            $"/api/internship-applications/request/{command.BusinessId}",
            ResponseBuilder.Success(201)
                .AddData(new { businessId = command.BusinessId })
                .AddMessage("İşletme öğrenci talebi oluşturuldu.")
                .Build());
    }
}
