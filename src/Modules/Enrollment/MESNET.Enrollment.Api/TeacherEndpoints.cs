using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Enrollment.Api;

public static class TeacherEndpoints
{
    public static IEndpointRouteBuilder MapTeacherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teachers").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Institution.Staff);
        group.MapGet("/{teacherId:guid}", Get).RequireAuthorization(Permissions.Institution.View);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Institution.View);

        return app;
    }

    private static async Task<IResult> Post(RegisterTeacher command, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<TeacherProfileDto>(command);
        return Results.Created($"/api/teachers/{dto.Id}",
            ResponseBuilder.Success(201)
                .AddData(dto)
                .AddMessage("Öğretmen kaydedildi.")
                .Build());
    }

    private static async Task<IResult> Get(Guid teacherId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<TeacherProfileDto?>(new GetTeacherProfile(teacherId));
        if (dto is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage($"Öğretmen bulunamadı: {teacherId}").Build());

        return Results.Ok(ResponseBuilder.Success().AddData(dto).Build());
    }

    private static async Task<IResult> GetAll(Guid? institutionId, IMessageBus bus)
    {
        var dtos = await bus.InvokeAsync<IReadOnlyList<TeacherProfileDto>>(new ListTeachers(institutionId));
        return Results.Ok(ResponseBuilder.Success().AddData(dtos).Build());
    }
}
