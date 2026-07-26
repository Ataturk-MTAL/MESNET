using System.Reflection;
using MESNET.Common.Infrastructure.Security;
using MESNET.Coordination.Application.Handlers;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Kapsam kontrolünün <b>nerede uygulandığı</b> regresyonu (#126).
///
/// <para>Karar: <b>okuma açık, yazma kapalı.</b> Alan şefi başka alanın dağıtımını
/// görebilir (koordinasyon bütününü görmek işe yarar), değiştiremez.</para>
///
/// <para>Kontrol yalnız <see cref="ICurrentUserService"/> üzerinden erişilebildiği için
/// handler imzasındaki bu parametre, kapsam kapısının varlığının güvenilir göstergesidir.
/// Yeni bir alan-bazlı yazma handler'ı eklenip kapı unutulursa ya da bir okuma handler'ına
/// kapı sızarsa bu test kırılır.</para>
/// </summary>
public sealed class BranchScopeHandlerCoverageTests
{
    /// <summary>Alan bazlı yazma yapan ve kapsam kapısı taşıması ZORUNLU handler'lar.</summary>
    private static readonly Type[] BranchScopedWriteHandlers =
    [
        typeof(UpdateBranchAssignedHoursHandler),
        typeof(UpdateBusinessAssignedHoursHandler),
        typeof(AssignBusinessToTeacherHandler),
        typeof(UnassignBusinessFromTeacherHandler),
        typeof(UnassignBusinessSlotHandler),
        typeof(UpsertBranchWorkloadConfigHandler),
    ];

    /// <summary>
    /// Alan kodu alan ama SALT OKUNUR olan handler'lar — kapsam kapısı taşımamalıdır.
    /// </summary>
    private static readonly Type[] BranchScopedReadHandlers =
    [
        typeof(ListBusinessesForAssignmentHandler),
        typeof(GetCoordinationSummaryHandler),
        typeof(GetAllTeachersOverviewHandler),
        typeof(GetBusinessClustersHandler),
        typeof(SuggestAssignedHoursHandler),
        typeof(GetBranchWorkloadConfigHandler),
        typeof(GetAssignmentHistoryHandler),
    ];

    [Fact]
    public void Alan_bazli_yazma_handlerlari_kapsam_kapisi_tasir()
    {
        foreach (var handler in BranchScopedWriteHandlers)
        {
            TakesCurrentUserService(handler)
                .ShouldBeTrue($"{handler.Name} alan bazlı yazma yapar, kapsam kontrolü ZORUNLUDUR (#126).");
        }
    }

    [Fact]
    public void Okuma_handlerlari_kapsam_kontrolune_tabi_degildir()
    {
        foreach (var handler in BranchScopedReadHandlers)
        {
            TakesCurrentUserService(handler)
                .ShouldBeFalse($"{handler.Name} salt okunurdur; alan şefi başka alanı GÖREBİLMELİDİR (#126).");
        }
    }

    private static bool TakesCurrentUserService(Type handlerType) =>
        handlerType
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.Name is "Handle" or "HandleAsync")
            .SelectMany(m => m.GetParameters())
            .Any(p => p.ParameterType == typeof(ICurrentUserService));
}
