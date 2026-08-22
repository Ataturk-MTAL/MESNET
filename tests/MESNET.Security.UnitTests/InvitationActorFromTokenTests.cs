using System.Reflection;
using MESNET.Common.Infrastructure.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Handlers;
using MESNET.Security.Core.Entities;
using MESNET.Security.Shared.Events;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Davet akışında denetim aktörünün <b>nereden geldiği</b> regresyonu (#137).
///
/// <para>Daveti kimin oluşturduğu/onayladığı/reddettiği istek gövdesinden okunuyordu.
/// Davet onayı bir yetki kararıdır: onayı yapan istemcinin, onaylayanın adını kendisi
/// yazması denetim izini değersiz kılar. Aktör artık token'ın <c>sub</c> claim'inden
/// gelir ve alan komut kaydından tamamen kaldırılmıştır.</para>
///
/// <para>Yapısal test tercih edilir: alan tipte yoksa gövdeye ne konursa konsun
/// bağlanacak bir şey yoktur. Bkz. Coordination tarafındaki eş test.</para>
/// </summary>
public sealed class InvitationActorFromTokenTests
{
    private static readonly string[] ActorFieldNames =
    [
        "CreatedByName", "ApprovedByName", "RejectedByName", "RequestedByName",
        "CreatedBy", "ApprovedBy", "RejectedBy", "UpdatedBy",
    ];

    private static IEnumerable<Type> CommandTypes =>
        typeof(CreateInvitation).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && t.Namespace == typeof(CreateInvitation).Namespace);

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

    [Theory]
    [InlineData(typeof(CreateInvitationHandler))]
    [InlineData(typeof(ApproveInvitationHandler))]
    [InlineData(typeof(RejectInvitationHandler))]
    public void Aktor_damgalayan_handlerlar_token_servisini_alir(Type handler)
    {
        var takesCurrentUser = handler
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.Name is "Handle" or "HandleAsync")
            .SelectMany(m => m.GetParameters())
            .Any(p => p.ParameterType == typeof(ICurrentUserService));

        takesCurrentUser.ShouldBeTrue(
            $"{handler.Name} denetim aktörü yazar; aktör token'dan alınmak ZORUNDADIR (#137).");
    }

    [Fact]
    public void Saklanan_aktor_alanlari_kimliktir_ad_degildir()
    {
        typeof(UserInvitation).GetProperty("CreatedById")!.PropertyType.ShouldBe(typeof(Guid?));
        typeof(UserInvitation).GetProperty("ApprovedById")!.PropertyType.ShouldBe(typeof(Guid?));
        typeof(UserInvitation).GetProperty("RejectedById")!.PropertyType.ShouldBe(typeof(Guid?));

        typeof(InvitationApproved).GetProperty("ApprovedById")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(InvitationRejected).GetProperty("RejectedById")!.PropertyType.ShouldBe(typeof(Guid));
    }

    [Fact]
    public void Eski_serbest_metin_aktor_alanlari_geri_gelmez()
    {
        typeof(UserInvitation).GetProperty("CreatedByName").ShouldBeNull();
        typeof(UserInvitation).GetProperty("ApprovedByName").ShouldBeNull();
        typeof(UserInvitation).GetProperty("RejectedByName").ShouldBeNull();
        typeof(InvitationApproved).GetProperty("ApprovedByName").ShouldBeNull();
        typeof(InvitationRejected).GetProperty("RejectedByName").ShouldBeNull();
    }

    /// <summary>
    /// Modüller arası olayda ad TAŞINMAZ (#137): ad taşınırsa tüketen modül onu saklar ve
    /// kaynak modüldeki ad değişince sessizce bayatlar. Kimlik taşınır, ad her modülde
    /// kendi <c>UserNameView</c>'ından okuma anında çözülür.
    /// </summary>
    [Fact]
    public void Kullanici_adi_olayi_kimlik_ve_ad_tasir()
    {
        typeof(UserDisplayNameUpserted).GetProperty("UserId")!.PropertyType.ShouldBe(typeof(Guid));
        typeof(UserDisplayNameUpserted).GetProperty("FullName")!.PropertyType.ShouldBe(typeof(string));
    }
}
