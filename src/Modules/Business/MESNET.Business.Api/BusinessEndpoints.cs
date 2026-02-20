using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Errors;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Core.Enums;
using MESNET.Business.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Business.Api;

public static class BusinessEndpoints
{
    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/businesses").WithTags("Business").RequireAuthorization();
        group.MapGet("/{businessId:guid}", Get).RequireAuthorization(Permissions.Company.View);
        group.MapPost("/", Post).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/self-register", PostSelfRegister);  // Authenticate kullanıcı — CompanyManager kendi kaydeder
        group.MapPut("/{businessId:guid}", Put).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/approve", PostApprove).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/reject", PostReject).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/deactivate", PostDeactivate).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/activate", PostActivate).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/close", PostClose).RequireAuthorization(Permissions.Company.Manage);
        return app;
    }

    private static async Task<IResult> Get(Guid businessId, IQuerySession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(business.ToDto())
            .Build());
    }

    private static async Task<IResult> Post(
        RegisterBusiness command, IDocumentSession session, IMessageBus bus)
    {
        var business = new Core.Entities.Business
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Name = command.Name,
            Address = command.Address,
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            Website = command.Website,
            PersonnelCount = command.PersonnelCount,
            Location = command.Location,
            Source = RegistrationSource.InstitutionRegistered,
            Status = BusinessStatus.Active,
            Capacity = new Core.ValueObjects.BusinessCapacity
            {
                TotalSlots = command.TotalSlots
            },
            ApprovedAt = DateTime.UtcNow
        };

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessRegistered(business.Id, business.Name, business.Location, business.Source));

        return Results.Created($"/api/businesses/{business.Id}",
            ResponseBuilder.Success(201)
                .AddData(business.ToDto())
                .AddMessage("İşletme başarıyla kaydedildi.")
                .Build());
    }

    private static async Task<IResult> PostSelfRegister(
        SelfRegisterBusiness command, IDocumentSession session, IMessageBus bus)
    {
        var business = new Core.Entities.Business
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Name = command.BusinessName,
            Address = command.Address,
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            Website = command.Website,
            PersonnelCount = command.PersonnelCount,
            Location = command.Location,
            Source = RegistrationSource.SelfRegistered,
            Status = BusinessStatus.PendingApproval,
            Capacity = new Core.ValueObjects.BusinessCapacity
            {
                TotalSlots = command.TotalSlots
            },
            Representatives =
            [
                new Core.ValueObjects.BusinessRepresentative
                {
                    KeycloakId = command.KeycloakId,
                    FullName = command.FullName,
                    PhoneNumber = command.RepresentativePhone,
                    Email = command.RepresentativeEmail
                }
            ]
        };

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessRegistered(business.Id, business.Name, business.Location, business.Source));
        await bus.PublishAsync(new BusinessApprovalRequested(business.Id, business.Name));

        return Results.Created($"/api/businesses/{business.Id}",
            ResponseBuilder.Success(201)
                .AddData(business.ToDto())
                .AddMessage("İşletme kaydı alındı, onay bekleniyor.")
                .Build());
    }

    private static async Task<IResult> Put(
        Guid businessId, UpdateBusinessInfo command, IDocumentSession session, IMessageBus bus)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        business.Name = command.Name;
        business.Address = command.Address;
        business.PhoneNumber = command.PhoneNumber;
        business.Email = command.Email;
        business.Website = command.Website;
        business.PersonnelCount = command.PersonnelCount;
        business.Location = command.Location;

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessUpdated(business.Id, business.Name, business.Location));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme bilgileri güncellendi.")
            .Build());
    }

    private static async Task<IResult> PostApprove(
        Guid businessId, IDocumentSession session, IMessageBus bus, ICurrentUserService currentUser)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        if (!business.Status.CanTransitionTo(BusinessStatus.Active))
        {
            var error = BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Active.Slug);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }

        business.Status = BusinessStatus.Active;
        business.ApprovedAt = DateTime.UtcNow;

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessApproved(business.Id, currentUser.GetFullName(), business.ApprovedAt.Value));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme onaylandı.")
            .Build());
    }

    private static async Task<IResult> PostReject(
        Guid businessId, RejectBusiness command, IDocumentSession session, IMessageBus bus)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        if (!business.Status.CanTransitionTo(BusinessStatus.Rejected))
        {
            var error = BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Rejected.Slug);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }

        business.Status = BusinessStatus.Rejected;

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessRejected(business.Id, command.Reason));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme reddedildi.")
            .Build());
    }

    private static async Task<IResult> PostDeactivate(
        Guid businessId, DeactivateBusiness command, IDocumentSession session, IMessageBus bus)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        if (!business.Status.CanTransitionTo(BusinessStatus.Inactive))
        {
            var error = BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Inactive.Slug);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }

        business.Status = BusinessStatus.Inactive;

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessDeactivated(business.Id, command.Reason));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme pasife alındı.")
            .Build());
    }

    private static async Task<IResult> PostActivate(
        Guid businessId, IDocumentSession session, IMessageBus bus)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        if (!business.Status.CanTransitionTo(BusinessStatus.Active))
        {
            var error = BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Active.Slug);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }

        business.Status = BusinessStatus.Active;

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessActivated(business.Id));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme aktifleştirildi.")
            .Build());
    }

    private static async Task<IResult> PostClose(
        Guid businessId, IDocumentSession session, IMessageBus bus)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        if (!business.Status.CanTransitionTo(BusinessStatus.Closed))
        {
            var error = BusinessErrors.InvalidTransition(business.Status.Slug, BusinessStatus.Closed.Slug);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }

        business.Status = BusinessStatus.Closed;
        business.ClosedAt = DateTime.UtcNow;

        session.Store(business);
        await session.SaveChangesAsync();

        await bus.PublishAsync(new BusinessClosed(business.Id, business.ClosedAt.Value));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme kapatıldı.")
            .Build());
    }
}
