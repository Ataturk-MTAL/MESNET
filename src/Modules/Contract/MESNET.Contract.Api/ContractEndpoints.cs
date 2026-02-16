using Marten;
using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Dtos;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Application.Extensions;
using MESNET.Contract.Application.Queries;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace MESNET.Contract.Api;

public static class ContractEndpoints
{
    [WolverinePost("/api/contracts")]
    public static async Task<IResult> Post(
        CreateContract command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<ContractCreated>>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Created(
            $"/api/contracts/{result.Value.ContractId}",
            ResponseBuilder.Success(201)
                .AddData(new { contractId = result.Value.ContractId })
                .AddMessage("Sözleşme oluşturuldu.")
                .Build());
    }

    [WolverinePost("/api/contracts/{contractId}/submit")]
    public static async Task<IResult> PostSubmit(
        Guid contractId, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<ContractSubmittedForSignature>>(
            new SubmitContractForSignature(contractId));

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme imzaya gönderildi.")
            .Build());
    }

    [WolverinePost("/api/contracts/{contractId}/sign")]
    public static async Task<IResult> PostSign(
        Guid contractId, SignContract command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<object>>(command with { ContractId = contractId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme imzalandı.")
            .Build());
    }

    [WolverinePost("/api/contracts/{contractId}/activate")]
    public static async Task<IResult> PostActivate(
        Guid contractId, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<ContractActivated>>(
            new ActivateContract(contractId));

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme aktifleştirildi.")
            .Build());
    }

    [WolverinePost("/api/contracts/{contractId}/suspend")]
    public static async Task<IResult> PostSuspend(
        Guid contractId, SuspendContract command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<ContractSuspended>>(
            command with { ContractId = contractId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme askıya alındı.")
            .Build());
    }

    [WolverinePost("/api/contracts/{contractId}/resume")]
    public static async Task<IResult> PostResume(
        Guid contractId, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<ContractResumed>>(
            new ResumeContract(contractId));

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme devam ettirildi.")
            .Build());
    }

    [WolverinePost("/api/contracts/{contractId}/terminate")]
    public static async Task<IResult> PostTerminate(
        Guid contractId, TerminateContract command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<ContractTerminated>>(
            command with { ContractId = contractId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme feshedildi.")
            .Build());
    }

    [WolverinePost("/api/contracts/{contractId}/complete")]
    public static async Task<IResult> PostComplete(
        Guid contractId, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<ContractCompleted>>(
            new CompleteContract(contractId));

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sözleşme tamamlandı.")
            .Build());
    }

    [WolverineGet("/api/contracts/{contractId}")]
    public static async Task<IResult> Get(
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

    [WolverineGet("/api/contracts")]
    public static async Task<IResult> GetAll(
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
            queryable = queryable.Where(c => c.Status == contractStatus);

        var contracts = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(contracts.Select(c => c.ToDto()).ToList())
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId}/upload-signed — Islak imzalı sözleşme yükle
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/contracts/{contractId}/upload-signed")]
    public static async Task<IResult> PostUploadSigned(
        Guid contractId, HttpRequest request, IMessageBus bus)
    {
        // Manuel form parsing (Wolverine IFormFile binding desteklemez)
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

        var command = new UploadSignedContractDocument(
            contractId,
            documentFile,
            uploadedBy);

        var result = await bus.InvokeAsync<Result<SignedContractDocumentUploaded>>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Islak imzalı sözleşme yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId}/upload-termination — Islak imzalı fesih belgesi yükle
    // ────────────────────────────────────────────────────────────────────────────────
    [WolverinePost("/api/contracts/{contractId}/upload-termination")]
    public static async Task<IResult> PostUploadTermination(
        Guid contractId, HttpRequest request, IMessageBus bus)
    {
        // Manuel form parsing (Wolverine IFormFile binding desteklemez)
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

        var command = new UploadTerminationDocument(
            contractId,
            documentFile,
            uploadedBy);

        var result = await bus.InvokeAsync<Result<TerminationDocumentUploaded>>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Islak imzalı fesih belgesi yüklendi.")
            .Build());
    }
}
