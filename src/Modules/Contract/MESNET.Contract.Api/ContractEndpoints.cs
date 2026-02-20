using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Application.Extensions;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Contract.Api;

public static class ContractEndpoints
{
    public static void MapContractEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contracts").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Internship.Contract);
        group.MapPost("/{contractId:guid}/submit", PostSubmit).RequireAuthorization(Permissions.Internship.Contract);
        group.MapPost("/{contractId:guid}/sign", PostSign).RequireAuthorization(Permissions.Internship.Contract);
        group.MapPost("/{contractId:guid}/activate", PostActivate).RequireAuthorization(Permissions.Internship.Contract);
        group.MapPost("/{contractId:guid}/suspend", PostSuspend).RequireAuthorization(Permissions.Internship.Manage);
        group.MapPost("/{contractId:guid}/resume", PostResume).RequireAuthorization(Permissions.Internship.Manage);
        group.MapPost("/{contractId:guid}/terminate", PostTerminate).RequireAuthorization(Permissions.Internship.Manage);
        group.MapPost("/{contractId:guid}/complete", PostComplete).RequireAuthorization(Permissions.Internship.Manage);
        group.MapGet("/{contractId:guid}", Get).RequireAuthorization(Permissions.Internship.Contract);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Internship.Contract);
        group.MapPost("/{contractId:guid}/upload-signed", PostUploadSigned).RequireAuthorization(Permissions.Document.Upload);
        group.MapPost("/{contractId:guid}/upload-termination", PostUploadTermination).RequireAuthorization(Permissions.Document.Upload);
    }

    private static async Task<IResult> Post(
        CreateContract command, IMessageBus bus)
    {
        var contractId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/contracts/{contractId}",
            ResponseBuilder.Success(201)
                .AddData(new { contractId })
                .AddMessage("Sözleşme oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> PostSubmit(
        Guid contractId, IMessageBus bus)
    {
        await bus.InvokeAsync(new SubmitContractForSignature(contractId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme imzaya gönderildi.")
            .Build());
    }

    private static async Task<IResult> PostSign(
        Guid contractId, SignContract command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InternshipContractId = contractId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme imzalandı.")
            .Build());
    }

    private static async Task<IResult> PostActivate(
        Guid contractId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ActivateContract(contractId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme aktifleştirildi.")
            .Build());
    }

    private static async Task<IResult> PostSuspend(
        Guid contractId, SuspendContract command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InternshipContractId = contractId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme askıya alındı.")
            .Build());
    }

    private static async Task<IResult> PostResume(
        Guid contractId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ResumeContract(contractId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme devam ettirildi.")
            .Build());
    }

    private static async Task<IResult> PostTerminate(
        Guid contractId, TerminateContract command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InternshipContractId = contractId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme feshedildi.")
            .Build());
    }

    private static async Task<IResult> PostComplete(
        Guid contractId, IMessageBus bus)
    {
        await bus.InvokeAsync(new CompleteContract(contractId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme tamamlandı.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid contractId, IQuerySession session)
    {
        var contract = await session.Events.AggregateStreamAsync<InternshipContract>(contractId);
        if (contract is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(ContractErrors.NotFound(contractId).Description)
                .AddErrors(ContractErrors.NotFound(contractId))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(contract.ToDto())
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, Guid? institutionId, string? status,
        IQuerySession session)
    {
        IQueryable<InternshipContract> queryable = session.Query<InternshipContract>();

        if (studentId.HasValue)
            queryable = queryable.Where(c => c.StudentId == studentId.Value);

        if (businessId.HasValue)
            queryable = queryable.Where(c => c.BusinessId == businessId.Value);

        if (institutionId.HasValue)
            queryable = queryable.Where(c => c.InstitutionId == institutionId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            ContractStatus.TryFromName(status, true, out var contractStatus))
            queryable = queryable.Where(c => c.Status.Name == contractStatus.Name);

        var contracts = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(contracts.Select(c => c.ToDto()).ToList())
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId}/upload-signed — Islak imzalı sözleşme yükle
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostUploadSigned(
        Guid contractId, HttpRequest request, IMessageBus bus)
    {
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

        var documentFile = form.Files.GetFile("DocumentFile");
        if (documentFile is null)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("DocumentFile eksik.")
                .Build());
        }

        var command = new UploadSignedContractDocument(contractId, documentFile, uploadedBy);
        await bus.InvokeAsync(command);

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Islak imzalı sözleşme yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId}/upload-termination — Islak imzalı fesih belgesi yükle
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostUploadTermination(
        Guid contractId, HttpRequest request, IMessageBus bus)
    {
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

        var documentFile = form.Files.GetFile("DocumentFile");
        if (documentFile is null)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("DocumentFile eksik.")
                .Build());
        }

        var command = new UploadTerminationDocument(contractId, documentFile, uploadedBy);
        await bus.InvokeAsync(command);

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Islak imzalı fesih belgesi yüklendi.")
            .Build());
    }
}
