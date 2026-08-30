using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Common.Shared.Pagination;
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
        // Küratörlü marka paleti kataloğu — kurum kapsamı yok (katalog her okulda aynı ve
        // koddan gelir). Aynı gerekçeyle /{institutionId:guid} kalıbından ÖNCE kaydedilir.
        group.MapGet("/brand-palettes", GetBrandPaletteCatalog)
            .RequireAuthorization(Permissions.Institution.View);

        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Institution.View);
        group.MapGet("/{institutionId:guid}", Get).RequireAuthorization(Permissions.Institution.View);
        // Yeni okul açmak KURUM ÜSTÜ bir iştir (ADR-0003 adım 6): institution:manage "kurum
        // yönetebilir" der, "yeni kiracı açabilir" demez. Ölçüldü: bu uç institution:manage
        // ile açıkken A okulunun müdürü ikinci bir okul yaratabiliyordu.
        group.MapPost("/", Post).RequireAuthorization(Permissions.Platform.TenantManage);
        group.MapPatch("/{institutionId:guid}", Patch).RequireAuthorization(Permissions.Institution.Manage);
        // Marka paleti kurum ayarıdır — FieldCatalog/AcademicPeriod ayar uçlarıyla aynı izin.
        group.MapPut("/{institutionId:guid}/brand-palette", PutBrandPalette)
            .RequireAuthorization(Permissions.Institution.Manage);
        group.MapPost("/{institutionId:guid}/staff", PostStaff).RequireAuthorization(Permissions.Institution.Staff);
        group.MapPut("/{institutionId:guid}/schedule-config", PutScheduleConfig).RequireAuthorization(Permissions.Institution.Manage);
        group.MapGet("/{institutionId:guid}/schedule-config", GetScheduleConfig).RequireAuthorization(Permissions.Institution.View);
        // Mevcut kullanıcıların alan kapsamı için tek seferlik geçiş adımı (#126) — idempotent
        // Personel kaydından kullanıcı hesabına backfill: alan kapsamı (#126) VE kurum
        // (kiracı anahtarı, ADR-0003 adım 2.1). Olay yeniden yayınlanır, Security tüketir —
        // modüller arası doğrudan veri yazma yoktur.
        group.MapPost("/staff/resync-branch-codes", PostResyncStaffBranchCodes).RequireAuthorization(Permissions.Institution.Staff);
        // Yöneticisi olmayan okullar (D2) — müdürlük panosu bootstrap iş listesi.
        group.MapGet("/unmanaged", GetUnmanaged).RequireAuthorization(Permissions.Institution.View);
        // Kurum ağacı geçişi — DAĞITIM ÖN KOŞULU, idempotent. Atlanırsa sessizdir: yollar boş
        // kalır ve il/ilçe yetkilisi hata değil BOŞ LİSTE görür. Kurum üstü bir iştir:
        // institution:manage "kurum yönetebilir" der, "bütün ağacı yeniden kurabilir" demez.
        group.MapPost("/rebuild-hierarchy", PostRebuildHierarchy)
            .RequireAuthorization(Permissions.Platform.TenantManage);

        return app;
    }

    /// <summary>
    /// Personel kayıtlarındaki alan bilgisini yeniden yayınlar; Security modülü tüketip
    /// kullanıcı kapsamını doldurur (#126). Alanı olmayan personel atlanır — bu beklenen
    /// durumdur, eksik değildir.
    /// </summary>
    /// <summary>
    /// Hedef kurum <b>aktörün claim'inden</b> gelir; kurum üstü aktör (<c>platform:tenant:manage</c>)
    /// <c>institutionId</c> sorgu parametresiyle başka okulu hedefleyebilir. Kapsam kararı
    /// <c>InstitutionScopeGuardMiddleware</c>'de: kendi okulu olmayan bir aktörün ya da yabancı
    /// hedef verenin isteği 422 döner.
    /// </summary>
    private static async Task<IResult> PostResyncStaffBranchCodes(
        Guid? institutionId, ICurrentUserService currentUser, IMessageBus bus)
    {
        var target = institutionId
            ?? currentUser.GetCurrentUser()?.InstitutionId
            ?? Guid.Empty;

        var result = await bus.InvokeAsync<ResyncStaffBranchCodesResult>(
            new ResyncStaffBranchCodes(target));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage(
                $"{result.TotalStaff} personel incelendi, {result.Published} kayıt için alan bilgisi yayınlandı " +
                $"({result.SkippedNoBranch} personelin alanı yok — normal, " +
                $"{result.SkippedNoKeycloakId} personel eşleştirilemedi).")
            .Build());
    }

    /// <summary>
    /// Kurum ağacını mevcut okul künyelerinden yeniden kurar. İdempotent — birden çok kez
    /// çağrılabilir.
    /// </summary>
    private static async Task<IResult> PostRebuildHierarchy(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<RebuildInstitutionHierarchyResult>(
            new RebuildInstitutionHierarchy());

        var uyari = result.SkippedNoProvince > 0
            ? $" {result.SkippedNoProvince} okulun il kodu yok; kapsamsız kaldılar ve hiçbir "
              + "il/ilçe yetkilisinin listesinde görünmezler."
            : string.Empty;

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage(
                $"Kurum ağacı kuruldu: {result.ProvincesCreated} il, {result.DistrictsCreated} ilçe "
                + $"müdürlüğü açıldı, {result.NodesUpdated} düğümün ağaç bilgisi yazıldı.{uyari}")
            .Build());
    }

    /// <summary>
    /// Seçilebilir palet listesi. Hex'ler buradan gelir — arayüz onları <b>ikinci kez
    /// tanımlamaz</b>; iki kopya olsaydı biri güncellenir, diğeri ölçülmemiş renkle kalırdı.
    /// </summary>
    private static async Task<IResult> GetBrandPaletteCatalog(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<BrandPaletteListDto>(new GetBrandPalettes());
        return Results.Ok(ResponseBuilder.Success().AddData(result.Items).Build());
    }

    /// <summary>
    /// Kurumun marka paletini değiştirir. Gövde yalnız <c>paletteName</c> taşır; hex kabul
    /// edilmez — küme kapalıdır, tanınmayan anahtar 422 döner.
    /// </summary>
    private static async Task<IResult> PutBrandPalette(
        Guid institutionId, SetInstitutionBrandPalette command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InstitutionId = institutionId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kurum teması güncellendi.")
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

    /// <summary>
    /// Görünür kurumların sayfalı listesi. Kapsam sorgunun İÇİNDE uygulanır (handler);
    /// uçta kimlik karşılaştırması yapılmaz.
    /// </summary>
    /// <param name="nodeType">
    /// <c>Province</c> / <c>District</c> / <c>School</c>. Verilmezse okullar döner.
    /// </param>
    /// <param name="parentId">Belirli bir düğümün doğrudan çocukları.</param>
    private static async Task<IResult> GetAll(
        string? nodeType = null,
        Guid? parentId = null,
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool descending = false,
        string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<InstitutionDto>>(
            new GetInstitutions(nodeType, parentId)
            {
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                Descending = descending,
                Search = search
            });

        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }

    /// <summary>
    /// Yöneticisi olmayan okullar (D2) — müdürlük panosu bootstrap iş listesi. Kapsam sorgunun
    /// İÇİNDE uygulanır (handler); uçta kimlik karşılaştırması yapılmaz.
    /// </summary>
    private static async Task<IResult> GetUnmanaged(
        int page = 1,
        int pageSize = 20,
        string? sortBy = null,
        bool descending = false,
        string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<InstitutionDto>>(
            new GetUnmanagedInstitutions
            {
                Page = page,
                PageSize = pageSize,
                SortBy = sortBy,
                Descending = descending,
                Search = search
            });

        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
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
