using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Shared.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Enrollment.Api;

public static class TeacherEndpoints
{
    public static IEndpointRouteBuilder MapTeacherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teachers");

        group.MapPost("/", Post);
        group.MapGet("/{teacherId:guid}", Get);
        group.MapGet("/", GetAll);

        return app;
    }

    private static async Task<IResult> Post(
        RegisterTeacher command, IDocumentSession session, IMessageBus bus)
    {
        var teacher = new TeacherProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName
        };

        session.Store(teacher);

        await bus.PublishAsync(
            new TeacherRegistered(teacher.Id, teacher.FullName, teacher.InstitutionId));

        return Results.Created($"/api/teachers/{teacher.Id}",
            ResponseBuilder.Success(201)
                .AddData(teacher.ToDto())
                .AddMessage("Öğretmen kaydedildi.")
                .Build());
    }

    private static async Task<IResult> Get(Guid teacherId, IQuerySession session)
    {
        var teacher = await session.LoadAsync<TeacherProfile>(teacherId);
        if (teacher is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(EnrollmentErrors.TeacherNotFound(teacherId).Description)
                .AddErrors(EnrollmentErrors.TeacherNotFound(teacherId))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(teacher.ToDto())
            .Build());
    }

    private static async Task<IResult> GetAll(Guid? institutionId, IQuerySession session)
    {
        IQueryable<TeacherProfile> queryable = session.Query<TeacherProfile>();

        if (institutionId.HasValue)
            queryable = queryable.Where(t => t.InstitutionId == institutionId.Value);

        var teachers = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(teachers.Select(t => t.ToDto()).ToList())
            .Build());
    }
}
