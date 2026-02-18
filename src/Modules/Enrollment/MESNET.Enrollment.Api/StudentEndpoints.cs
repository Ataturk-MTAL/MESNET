using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Enrollment.Api;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Student.Manage);
        group.MapPut("/{studentId:guid}", Put).RequireAuthorization(Permissions.Student.Manage);
        group.MapGet("/{studentId:guid}", Get).RequireAuthorization(Permissions.Student.View);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Student.View);

        return app;
    }

    private static async Task<IResult> Post(
        RegisterStudent command, IDocumentSession session, IMessageBus bus)
    {
        var student = new StudentProfile
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            KeycloakUserId = command.KeycloakUserId,
            FullName = command.FullName,
            BranchCode = command.BranchCode,
            BranchName = command.BranchName,
            ClassYear = command.ClassYear,
            Section = command.Section
        };

        session.Store(student);

        await bus.PublishAsync(
            new StudentRegistered(student.Id, student.FullName, student.InstitutionId, student.BranchCode));

        return Results.Created($"/api/students/{student.Id}",
            ResponseBuilder.Success(201)
                .AddData(student.ToDto())
                .AddMessage("Öğrenci kaydedildi.")
                .Build());
    }

    private static async Task<IResult> Put(
        Guid studentId, UpdateStudentProfile command, IDocumentSession session)
    {
        var student = await session.LoadAsync<StudentProfile>(studentId);
        if (student is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(EnrollmentErrors.StudentNotFound(studentId).Description)
                .AddErrors(EnrollmentErrors.StudentNotFound(studentId))
                .Build());

        student.FullName = command.FullName;
        student.BranchCode = command.BranchCode;
        student.BranchName = command.BranchName;
        student.ClassYear = command.ClassYear;
        student.Section = command.Section;

        session.Store(student);

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Öğrenci profili güncellendi.")
            .Build());
    }

    private static async Task<IResult> Get(Guid studentId, IQuerySession session)
    {
        var student = await session.LoadAsync<StudentProfile>(studentId);
        if (student is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(EnrollmentErrors.StudentNotFound(studentId).Description)
                .AddErrors(EnrollmentErrors.StudentNotFound(studentId))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(student.ToDto())
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? institutionId, string? branchCode, string? section, string? status, IQuerySession session)
    {
        IQueryable<StudentProfile> queryable = session.Query<StudentProfile>();

        if (institutionId.HasValue)
            queryable = queryable.Where(s => s.InstitutionId == institutionId.Value);

        if (!string.IsNullOrWhiteSpace(branchCode))
            queryable = queryable.Where(s => s.BranchCode == branchCode);

        if (!string.IsNullOrWhiteSpace(section))
            queryable = queryable.Where(s => s.Section == section);

        if (!string.IsNullOrWhiteSpace(status) &&
            StudentStatus.TryFromName(status, true, out var studentStatus))
            queryable = queryable.Where(s => s.Status == studentStatus);

        var students = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(students.Select(s => s.ToDto()).ToList())
            .Build());
    }
}
