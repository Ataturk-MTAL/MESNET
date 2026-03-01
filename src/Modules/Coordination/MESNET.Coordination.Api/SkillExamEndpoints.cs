using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Coordination.Api;

public static class SkillExamEndpoints
{
    public static IEndpointRouteBuilder MapSkillExamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coordination/skill-exams")
            .WithTags("SkillExam").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapPut("/{examId:guid}", Put).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapGet("/{examId:guid}", Get).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Coordinator.Visit);

        return app;
    }

    private static async Task<IResult> Post(
        CreateSkillExam command, IMessageBus bus)
    {
        var examId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/coordination/skill-exams/{examId}",
            ResponseBuilder.Success(201)
                .AddData(new { examId })
                .AddMessage("Beceri sınavı oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> Put(
        Guid examId, UpdateSkillExam command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { ExamId = examId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Beceri sınavı güncellendi.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid examId, IMessageBus bus)
    {
        var exam = await bus.InvokeAsync<SkillExam>(new GetSkillExam(examId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(exam)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, Guid? academicPeriodId, int? academicYear, string? semester,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<SkillExam>>(
            new ListSkillExams(studentId, businessId, academicPeriodId, academicYear, semester)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }
}
