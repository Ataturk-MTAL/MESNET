using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Dtos;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Application.Queries;
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

        // İşletme fesih talebi akışı
        group.MapPost("/{contractId:guid}/request-termination", PostRequestTermination)
            .RequireAuthorization(Permissions.Company.Student);
        group.MapPost("/{contractId:guid}/reject-termination", PostRejectTermination)
            .RequireAuthorization(Permissions.Internship.Approve);
        group.MapGet("/{contractId:guid}", Get).RequireAuthorization(Permissions.Internship.Contract);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Internship.Contract);

        // Evrak yükleme — tek genel endpoint, DocumentType form alanıyla nitelik belirtilir
        group.MapPost("/{contractId:guid}/documents", PostUploadDocument)
            .RequireAuthorization(Permissions.Document.Upload)
            .DisableAntiforgery();
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
        Guid contractId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<InternshipContractDto?>(new GetContract(contractId));
        if (dto is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(ContractErrors.NotFound(contractId).Description)
                .AddErrors(ContractErrors.NotFound(contractId))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(dto)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, Guid? institutionId, Guid? academicPeriodId, string? status,
        IMessageBus bus)
    {
        var query = new ListContracts(studentId, businessId, institutionId, academicPeriodId, status);
        var contracts = await bus.InvokeAsync<IReadOnlyList<InternshipContractDto>>(query);
        return Results.Ok(ResponseBuilder.Success()
            .AddData(contracts)
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId}/documents — Sözleşmeye evrak yükle
    // Form fields: DocumentFile (IFormFile), DocumentType (string), Description (string?), UploadedBy (string)
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostUploadDocument(
        Guid contractId, HttpRequest request, IMessageBus bus)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Multipart form-data bekleniyor.")
                .Build());

        var form = await request.ReadFormAsync();

        var uploadedBy = form["UploadedBy"].ToString();
        if (string.IsNullOrWhiteSpace(uploadedBy))
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("UploadedBy geçersiz veya eksik.")
                .Build());

        var documentType = form["DocumentType"].ToString();
        if (string.IsNullOrWhiteSpace(documentType))
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("DocumentType geçersiz veya eksik. Geçerli değerler: SignedContract, TerminationLetter, Other")
                .Build());

        var documentFile = form.Files.GetFile("DocumentFile");
        if (documentFile is null)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("DocumentFile eksik.")
                .Build());

        var description = form["Description"].ToString();

        var command = new UploadContractDocument(
            contractId,
            documentFile,
            documentType,
            string.IsNullOrWhiteSpace(description) ? null : description,
            uploadedBy);

        await bus.InvokeAsync(command);

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Evrak yüklendi.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId}/request-termination — İşletme fesih talebi
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostRequestTermination(
        Guid contractId, RequestTermination command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InternshipContractId = contractId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Fesih talebi oluşturuldu. Kurum onayı bekleniyor.")
            .Build());
    }

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId}/reject-termination — Fesih talebini reddet
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostRejectTermination(
        Guid contractId, RejectTermination command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InternshipContractId = contractId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Fesih talebi reddedildi. Sözleşme aktif duruma döndü.")
            .Build());
    }
}
