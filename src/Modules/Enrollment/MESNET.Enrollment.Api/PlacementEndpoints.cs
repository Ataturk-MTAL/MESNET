using Marten;
using MESNET.Common.Shared;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Errors;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.ReadModels;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Shared.Events;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace MESNET.Enrollment.Api;

public static class PlacementEndpoints
{
    [WolverinePost("/api/placements")]
    public static async Task<IResult> Post(
        PlaceStudent command, IDocumentSession session, IMessageBus bus)
    {
        var student = await session.LoadAsync<StudentProfile>(command.StudentId);
        if (student is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(EnrollmentErrors.StudentNotFound(command.StudentId).Description)
                .AddErrors(EnrollmentErrors.StudentNotFound(command.StudentId))
                .Build());

        if (!student.Status.CanTransitionTo(StudentStatus.Placed))
        {
            var error = EnrollmentErrors.InvalidTransition("Öğrenci", student.Status.Slug, StudentStatus.Placed.Slug);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }

        var business = await session.LoadAsync<BusinessProfileView>(command.BusinessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(EnrollmentErrors.BusinessNotFound(command.BusinessId).Description)
                .AddErrors(EnrollmentErrors.BusinessNotFound(command.BusinessId))
                .Build());

        if (!business.IsActive)
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(EnrollmentErrors.BusinessNotActive.Description)
                .AddErrors(EnrollmentErrors.BusinessNotActive)
                .Build());

        if (business.AvailableCapacity <= 0)
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(EnrollmentErrors.BusinessCapacityFull.Description)
                .AddErrors(EnrollmentErrors.BusinessCapacityFull)
                .Build());

        var placement = new InternshipPlacement
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            TeacherId = command.TeacherId,
            Source = ApplicationSource.InstitutionAssignment
        };

        student.Status = StudentStatus.Placed;

        session.Store(placement);
        session.Store(student);

        await bus.PublishAsync(
            new StudentPlaced(
                placement.Id,
                placement.StudentId,
                placement.BusinessId,
                placement.InstitutionId,
                placement.PlacedAt));

        return Results.Created($"/api/placements/{placement.Id}",
            ResponseBuilder.Success(201)
                .AddData(placement.ToDto())
                .AddMessage("Öğrenci yerleştirildi.")
                .Build());
    }

    [WolverinePost("/api/placements/{placementId}/transfer")]
    public static async Task<IResult> PostTransfer(
        Guid placementId, TransferStudent command, IDocumentSession session, IMessageBus bus)
    {
        var oldPlacement = await session.LoadAsync<InternshipPlacement>(placementId);
        if (oldPlacement is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(EnrollmentErrors.PlacementNotFound(placementId).Description)
                .AddErrors(EnrollmentErrors.PlacementNotFound(placementId))
                .Build());

        if (!oldPlacement.Status.CanTransitionTo(PlacementStatus.Transferred))
        {
            var error = EnrollmentErrors.InvalidTransition("Yerleştirme", oldPlacement.Status.Slug, PlacementStatus.Transferred.Slug);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }

        var newBusiness = await session.LoadAsync<BusinessProfileView>(command.NewBusinessId);
        if (newBusiness is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(EnrollmentErrors.BusinessNotFound(command.NewBusinessId).Description)
                .AddErrors(EnrollmentErrors.BusinessNotFound(command.NewBusinessId))
                .Build());

        if (!newBusiness.IsActive)
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(EnrollmentErrors.BusinessNotActive.Description)
                .AddErrors(EnrollmentErrors.BusinessNotActive)
                .Build());

        if (newBusiness.AvailableCapacity <= 0)
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(EnrollmentErrors.BusinessCapacityFull.Description)
                .AddErrors(EnrollmentErrors.BusinessCapacityFull)
                .Build());

        oldPlacement.Status = PlacementStatus.Transferred;
        oldPlacement.TransferredAt = DateTime.UtcNow;
        oldPlacement.TransferReason = command.Reason;

        var newPlacement = new InternshipPlacement
        {
            Id = Guid.NewGuid(),
            StudentId = oldPlacement.StudentId,
            BusinessId = command.NewBusinessId,
            InstitutionId = oldPlacement.InstitutionId,
            TeacherId = oldPlacement.TeacherId,
            Source = ApplicationSource.InstitutionAssignment
        };

        session.Store(oldPlacement);
        session.Store(newPlacement);

        await bus.PublishAsync(
            new StudentTransferred(
                oldPlacement.Id,
                oldPlacement.StudentId,
                oldPlacement.BusinessId,
                command.NewBusinessId,
                command.Reason));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Öğrenci transfer edildi.")
            .Build());
    }

    [WolverineGet("/api/placements/{placementId}")]
    public static async Task<IResult> Get(Guid placementId, IQuerySession session)
    {
        var placement = await session.LoadAsync<InternshipPlacement>(placementId);
        if (placement is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(EnrollmentErrors.PlacementNotFound(placementId).Description)
                .AddErrors(EnrollmentErrors.PlacementNotFound(placementId))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(placement.ToDto())
            .Build());
    }

    [WolverineGet("/api/placements")]
    public static async Task<IResult> GetAll(
        Guid? businessId, Guid? studentId, string? status, IQuerySession session)
    {
        IQueryable<InternshipPlacement> queryable = session.Query<InternshipPlacement>();

        if (businessId.HasValue)
            queryable = queryable.Where(p => p.BusinessId == businessId.Value);

        if (studentId.HasValue)
            queryable = queryable.Where(p => p.StudentId == studentId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            PlacementStatus.TryFromName(status, true, out var placementStatus))
            queryable = queryable.Where(p => p.Status == placementStatus);

        var placements = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(placements.Select(p => p.ToDto()).ToList())
            .Build());
    }
}
