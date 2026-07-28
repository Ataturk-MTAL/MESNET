using System.Reflection;
using MESNET.Common.Infrastructure.Security;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Handlers;
using MESNET.Coordination.Core.Aggregates;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Denetim aktörünün <b>nereden geldiği</b> regresyonu (#137).
///
/// <para>Kusur şuydu: <c>UpdatedBy</c> / <c>AssignedBy</c> gibi alanlar HTTP istek
/// gövdesinden okunuyordu. Bir işlemi yapan istemci, o işlemin denetim satırındaki
/// aktörü kendisi yazıyordu ve alan serbest metin olduğu için başkasının adını da
/// yazabiliyordu.</para>
///
/// <para><b>Neden sahte istek göndermek yerine yapısal test:</b> aktör alanı komut
/// kaydından tamamen kaldırıldığı için gövdeye ne konursa konsun bağlanacak bir alan
/// yoktur — sahtelemenin imkânsızlığı tipin şeklinden gelir. Canlı API'ye istek atan bir
/// test yalnız bugünkü tek bir uç için kanıt verirdi; buradaki kontrol komut kayıtlarının
/// TAMAMINI tarar, yani yarın eklenecek komut da kapsanır. Ayrıca altyapı gerektirmez,
/// CI'da her koşuda çalışır.</para>
///
/// <para>Alanı geri ekleyen ya da yeni bir komuta aktör alanı koyan biri, bunu sessizce
/// değil <b>kırmızı testle</b> öğrenir.</para>
/// </summary>
public sealed class ActorFromTokenTests
{
    /// <summary>
    /// Aktör anlamı taşıyan alan adları. Gövdeden okunmaları YASAK; token'dan damgalanırlar.
    /// </summary>
    private static readonly string[] ActorFieldNames =
    [
        "UpdatedBy", "AssignedBy", "UnassignedBy", "ApprovedBy", "RejectedBy",
        "CreatedBy", "PerformedBy", "ModifiedBy", "LastModifiedBy",
        "CreatedByName", "ApprovedByName", "RejectedByName", "RequestedByName",
    ];

    private static IEnumerable<Type> CommandTypes =>
        typeof(UpsertCoordinationConfig).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace == typeof(UpsertCoordinationConfig).Namespace);

    [Fact]
    public void Hicbir_komut_aktor_alani_tasimaz()
    {
        var offenders = CommandTypes
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => ActorFieldNames.Contains(p.Name))
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();

        offenders.ShouldBeEmpty(
            "Aktör alanı istek gövdesinden okunamaz — handler token'dan damgalamalıdır (#137). "
            + $"İhlal eden: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// Aktör damgalayan yazma handler'ları. Damga yalnız <see cref="ICurrentUserService"/>
    /// üzerinden alınabildiği için imzadaki bu parametre damganın varlığının göstergesidir.
    /// </summary>
    private static readonly Type[] ActorStampingHandlers =
    [
        typeof(UpsertCoordinationConfigHandler),
        typeof(UpsertBranchWorkloadConfigHandler),
        typeof(UpdateBranchAssignedHoursHandler),
        typeof(UpdateBusinessAssignedHoursHandler),
        typeof(UpsertTeacherScheduleHandler),
        typeof(AssignBusinessToTeacherHandler),
        typeof(AssignBusinessToFreeSlotHandler),
        typeof(UnassignBusinessFromTeacherHandler),
        typeof(UnassignBusinessSlotHandler),
    ];

    [Fact]
    public void Aktor_damgalayan_handlerlar_token_servisini_alir()
    {
        foreach (var handler in ActorStampingHandlers)
        {
            var takesCurrentUser = handler
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                .Where(m => m.Name is "Handle" or "HandleAsync")
                .SelectMany(m => m.GetParameters())
                .Any(p => p.ParameterType == typeof(ICurrentUserService));

            takesCurrentUser.ShouldBeTrue(
                $"{handler.Name} denetim aktörü yazar; aktör token'dan alınmak ZORUNDADIR (#137).");
        }
    }

    /// <summary>
    /// Saklanan alanlar <c>Guid</c> olmalıdır. Serbest metin ad saklamak, adın yazma
    /// anında istemciden gelmesine kapı açar; kimlik saklayıp adı okuma anında çözmek
    /// bu kapıyı kapatır.
    /// </summary>
    [Fact]
    public void Saklanan_aktor_alanlari_kimliktir_ad_degildir()
    {
        typeof(CoordinationConfig).GetProperty("UpdatedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(BranchWorkloadConfig).GetProperty("UpdatedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(BusinessCoordinationView).GetProperty("LastModifiedById")!.PropertyType.ShouldBe(typeof(Guid?));
        typeof(AssignmentHistoryEntry).GetProperty("PerformedById")!.PropertyType.ShouldBe(typeof(Guid));

        typeof(ScheduleCreated).GetProperty("UpdatedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(ScheduleUpdated).GetProperty("UpdatedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(TeacherSchedule).GetProperty("CreatedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(TeacherSchedule).GetProperty("UpdatedById")!.PropertyType.ShouldBe(typeof(Guid?));
    }

    /// <summary>
    /// Saklanan olay/belge alanları YENİDEN ADLANDIRILMIŞ olmalıdır — tipi yerinde
    /// değiştirmek veri kaybından öte, veri <b>bozulması</b> demektir.
    ///
    /// <para><c>ScheduleCreated</c> / <c>ScheduleUpdated</c> <c>shared.mt_events</c>
    /// içinde kalıcıdır ve her okumada replay edilir. Alan adı <c>UpdatedBy</c> kalıp tipi
    /// <c>Guid</c> yapılsaydı, saklı <c>"updatedBy": "admin"</c> değeri deserialize
    /// edilemez ve <c>TeacherSchedule</c> aggregate'inin replay'i <c>JsonException</c> ile
    /// TÜMDEN kırılırdı — yalnız denetim adı değil, ders programının kendisi okunamaz
    /// hâle gelirdi. Yeni ad eski JSON anahtarını sessizce yok sayar.</para>
    /// </summary>
    [Fact]
    public void Eski_serbest_metin_aktor_alanlari_geri_gelmez()
    {
        typeof(ScheduleCreated).GetProperty("UpdatedBy").ShouldBeNull(
            "Alan yeniden adlandırıldı; eski adı geri koymak saklı event JSON'unu "
            + "okunamaz kılar ve aggregate replay'ini kırar (#137).");

        typeof(ScheduleUpdated).GetProperty("UpdatedBy").ShouldBeNull();
        typeof(TeacherSchedule).GetProperty("CreatedBy").ShouldBeNull();
        typeof(TeacherSchedule).GetProperty("UpdatedBy").ShouldBeNull();
        typeof(BusinessCoordinationView).GetProperty("LastModifiedBy").ShouldBeNull();
        typeof(AssignmentHistoryEntry).GetProperty("PerformedBy").ShouldBeNull();
        typeof(CoordinationConfig).GetProperty("UpdatedBy").ShouldBeNull();
        typeof(BranchWorkloadConfig).GetProperty("UpdatedBy").ShouldBeNull();
    }
}
