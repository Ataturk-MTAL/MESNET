using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Shared.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Institution.Api;

public static class InstitutionEndpoints
{
    public static IEndpointRouteBuilder MapInstitutionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/institutions")
            .WithTags("Institution").RequireAuthorization();

        group.MapGet("/{institutionId:guid}", Get).RequireAuthorization(Permissions.Institution.View);
        group.MapPost("/", Post).RequireAuthorization(Permissions.Institution.Manage);
        group.MapPut("/{institutionId:guid}", Put).RequireAuthorization(Permissions.Institution.Manage);
        group.MapPost("/{institutionId:guid}/staff", PostStaff).RequireAuthorization(Permissions.Institution.Staff);
        group.MapPut("/{institutionId:guid}/schedule-config", PutScheduleConfig).RequireAuthorization(Permissions.Institution.Manage);
        group.MapGet("/{institutionId:guid}/schedule-config", GetScheduleConfig).RequireAuthorization(Permissions.Institution.View);

        return app;
    }

    private static async Task<IResult> Get(Guid institutionId, IQuerySession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(institutionId);
        if (institution is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.NotFound(institutionId).Description)
                .AddErrors(InstitutionErrors.NotFound(institutionId))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(institution.ToDto())
            .Build());
    }

    private static async Task<IResult> Post(
        CreateInstitution command, IDocumentSession session, IMessageBus bus)
    {
        var institution = new Core.Entities.Institution
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            InstitutionCode = command.InstitutionCode,
            FullName = command.FullName,
            Address = command.Address,
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            WebUrl = command.WebUrl,
            Location = command.Location
        };

        session.Store(institution);
        await session.SaveChangesAsync();

        await bus.PublishAsync(
            new InstitutionUpdated(institution.Id, institution.FullName, institution.Location));

        return Results.Created($"/api/institutions/{institution.Id}",
            ResponseBuilder.Success(201)
                .AddData(institution.ToDto())
                .AddMessage("Kurum oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> Put(
        Guid institutionId, UpdateInstitution command, IDocumentSession session, IMessageBus bus)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(institutionId);
        if (institution is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.NotFound(institutionId).Description)
                .AddErrors(InstitutionErrors.NotFound(institutionId))
                .Build());

        institution.FullName = command.FullName;
        institution.Address = command.Address;
        institution.PhoneNumber = command.PhoneNumber;
        institution.Email = command.Email;
        institution.WebUrl = command.WebUrl;
        institution.Location = command.Location;

        session.Store(institution);
        await session.SaveChangesAsync();

        await bus.PublishAsync(
            new InstitutionUpdated(institution.Id, institution.FullName, institution.Location));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kurum bilgileri güncellendi.")
            .Build());
    }

    private static async Task<IResult> PostStaff(
        Guid institutionId, AuthorizeStaff command, IDocumentSession session, IMessageBus bus)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(institutionId);
        if (institution is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.NotFound(institutionId).Description)
                .AddErrors(InstitutionErrors.NotFound(institutionId))
                .Build());

        var staff = new Core.ValueObjects.StaffMember
        {
            KeycloakId = command.KeycloakId,
            FullName = command.FullName,
            Role = command.Role,
            BranchCode = command.BranchCode
        };

        institution.Staff.Add(staff);
        session.Store(institution);
        await session.SaveChangesAsync();

        await bus.PublishAsync(
            new StaffAuthorized(institution.Id, staff.Id, staff.Role, staff.BranchCode));

        return Results.Created(
            $"/api/institutions/{institutionId}/staff/{staff.Id}",
            ResponseBuilder.Success(201)
                .AddData(new { staffId = staff.Id })
                .AddMessage("Personel yetkilendirildi.")
                .Build());
    }

    private static async Task<IResult> PutScheduleConfig(
        Guid institutionId,
        UpdateScheduleConfiguration command,
        IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(
            command with { InstitutionId = institutionId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Ders programı ayarları güncellendi.")
            .Build());
    }

    private static async Task<IResult> GetScheduleConfig(
        Guid institutionId,
        IQuerySession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(institutionId);
        if (institution is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.NotFound(institutionId).Description)
                .AddErrors(InstitutionErrors.NotFound(institutionId))
                .Build());

        if (institution.ScheduleConfig is null)
            return Results.Ok(ResponseBuilder.Success()
                .AddData(new { configured = false })
                .AddMessage("Ders programı ayarları henüz yapılmamış.")
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new
            {
                configured = true,
                dailyPeriodCount = institution.ScheduleConfig.DailyPeriodCount,
                updatedAt = institution.ScheduleConfig.UpdatedAt,
                updatedBy = institution.ScheduleConfig.UpdatedBy
            })
            .Build());
    }
}
