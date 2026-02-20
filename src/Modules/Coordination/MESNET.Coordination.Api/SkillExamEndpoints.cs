using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Commands;
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
        var result = await bus.InvokeAsync<Result<Guid>>(command);

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        var examId = result.Value;
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
        var result = await bus.InvokeAsync<Result>(command with { ExamId = examId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Beceri sınavı güncellendi.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid examId, IQuerySession session)
    {
        var exam = await session.LoadAsync<SkillExam>(examId);
        if (exam is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage($"Beceri sınavı bulunamadı: {examId}")
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(exam)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, int? academicYear, string? semester,
        IQuerySession session)
    {
        IQueryable<SkillExam> queryable = session.Query<SkillExam>();

        if (studentId.HasValue)
            queryable = queryable.Where(e => e.StudentId == studentId.Value);
        if (businessId.HasValue)
            queryable = queryable.Where(e => e.BusinessId == businessId.Value);
        if (academicYear.HasValue)
            queryable = queryable.Where(e => e.AcademicYear == academicYear.Value);

        var exams = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(exams)
            .Build());
    }
}
