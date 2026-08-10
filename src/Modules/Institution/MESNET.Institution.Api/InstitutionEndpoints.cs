using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Queries;
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

        // Ulusal referans verisi — kurum kapsamı yok, /{institutionId:guid} kalıbından ÖNCE
        // kaydedilir ki "provinces" bir Guid gibi ele alınmaya çalışılmasın.
        group.MapGet("/provinces", GetProvinceList).RequireAuthorization(Permissions.Institution.View);
        group.MapGet("/provinces/{provinceCode}/districts", GetDistrictList)
            .RequireAuthorization(Permissions.Institution.View);

        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Institution.View);
        group.MapGet("/{institutionId:guid}", Get).RequireAuthorization(Permissions.Institution.View);
        group.MapPost("/", Post).RequireAuthorization(Permissions.Institution.Manage);
        group.MapPatch("/{institutionId:guid}", Patch).RequireAuthorization(Permissions.Institution.Manage);
        group.MapPost("/{institutionId:guid}/staff", PostStaff).RequireAuthorization(Permissions.Institution.Staff);
        group.MapPut("/{institutionId:guid}/schedule-config", PutScheduleConfig).RequireAuthorization(Permissions.Institution.Manage);
        group.MapGet("/{institutionId:guid}/schedule-config", GetScheduleConfig).RequireAuthorization(Permissions.Institution.View);
        // Mevcut kullanıcıların alan kapsamı için tek seferlik geçiş adımı (#126) — idempotent
        // Personel kaydından kullanıcı hesabına backfill: alan kapsamı (#126) VE kurum
        // (kiracı anahtarı, ADR-0003 adım 2.1). Olay yeniden yayınlanır, Security tüketir —
        // modüller arası doğrudan veri yazma yoktur.
        group.MapPost("/staff/resync-branch-codes", PostResyncStaffBranchCodes).RequireAuthorization(Permissions.Institution.Staff);

        return app;
    }

    /// <summary>
    /// Personel kayıtlarındaki alan bilgisini yeniden yayınlar; Security modülü tüketip
    /// kullanıcı kapsamını doldurur (#126). Alanı olmayan personel atlanır — bu beklenen
    /// durumdur, eksik değildir.
    /// </summary>
    private static async Task<IResult> PostResyncStaffBranchCodes(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<ResyncStaffBranchCodesResult>(new ResyncStaffBranchCodes());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage(
                $"{result.TotalStaff} personel incelendi, {result.Published} kayıt için alan bilgisi yayınlandı " +
                $"({result.SkippedNoBranch} personelin alanı yok — normal, " +
                $"{result.SkippedNoKeycloakId} personel eşleştirilemedi).")
            .Build());
    }

    private static async Task<IResult> GetProvinceList(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<ProvinceListDto>(new GetProvinces());
        return Results.Ok(ResponseBuilder.Success().AddData(result.Items).Build());
    }

    private static async Task<IResult> GetDistrictList(string provinceCode, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<DistrictListDto>(new GetDistricts(provinceCode));
        return Results.Ok(ResponseBuilder.Success().AddData(result.Items).Build());
    }

    private static async Task<IResult> GetAll(IMessageBus bus)
    {
        var institutions = await bus.InvokeAsync<List<InstitutionDto>>(new GetInstitutions());
        return Results.Ok(ResponseBuilder.Success()
            .AddData(institutions)
            .Build());
    }

    private static async Task<IResult> Get(Guid institutionId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<InstitutionDto>(new GetInstitution(institutionId));
        return Results.Ok(ResponseBuilder.Success()
            .AddData(dto)
            .Build());
    }

    private static async Task<IResult> Post(CreateInstitution command, IMessageBus bus)
    {
        var institutionId = await bus.InvokeAsync<Guid>(command);
        return Results.Created($"/api/institutions/{institutionId}",
            ResponseBuilder.Success(201)
                .AddData(new { id = institutionId })
                .AddMessage("Kurum oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> Patch(Guid institutionId, UpdateInstitution command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InstitutionId = institutionId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kurum bilgileri güncellendi.")
            .Build());
    }

    private static async Task<IResult> PostStaff(Guid institutionId, AuthorizeStaff command, IMessageBus bus)
    {
        var staffId = await bus.InvokeAsync<Guid>(command with { InstitutionId = institutionId });
        return Results.Created(
            $"/api/institutions/{institutionId}/staff/{staffId}",
            ResponseBuilder.Success(201)
                .AddData(new { staffId })
                .AddMessage("Personel yetkilendirildi.")
                .Build());
    }

    private static async Task<IResult> PutScheduleConfig(
        Guid institutionId,
        UpdateScheduleConfiguration command,
        IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InstitutionId = institutionId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Ders programı ayarları güncellendi.")
            .Build());
    }

    private static async Task<IResult> GetScheduleConfig(Guid institutionId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<ScheduleConfigDto>(new GetScheduleConfig(institutionId));
        return Results.Ok(ResponseBuilder.Success()
            .AddData(dto)
            .Build());
    }
}
