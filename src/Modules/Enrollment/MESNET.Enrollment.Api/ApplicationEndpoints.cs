using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace MESNET.Enrollment.Api;

public static class ApplicationEndpoints
{
    [WolverinePost("/api/internship-applications")]
    public static async Task<IResult> Post(
        ApplyForInternship command, IDocumentSession session, IMessageBus bus)
    {
        var student = await session.LoadAsync<StudentProfile>(command.StudentId);
        if (student is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(EnrollmentErrors.StudentNotFound(command.StudentId).Description)
                .AddErrors(EnrollmentErrors.StudentNotFound(command.StudentId))
                .Build());

        if (!student.Status.CanTransitionTo(StudentStatus.Applied))
        {
            var error = EnrollmentErrors.InvalidTransition("Öğrenci", student.Status.Slug, StudentStatus.Applied.Slug);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }

        student.Status = StudentStatus.Applied;
        session.Store(student);

        await bus.PublishAsync(
            new InternshipApplied(
                student.Id,
                command.BusinessId,
                command.BranchCode,
                ApplicationSource.StudentApplication.Name));

        return Results.Created(
            $"/api/internship-applications/{student.Id}",
            ResponseBuilder.Success(201)
                .AddData(new { studentId = student.Id })
                .AddMessage("Staj başvurusu oluşturuldu.")
                .Build());
    }

    [WolverinePost("/api/internship-applications/request")]
    public static async Task<IResult> PostRequest(
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
