using System.Reflection;
using MESNET.Institution.Application.Security;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Guard'ın <b>şeklini</b> kilitler. Karar saf <c>InstitutionScopePolicy</c>'dedir ve orası
/// birim testiyle kapalı; burada kilitlenen şey, guard'ın o karara <b>ağaç aşamasını da</b>
/// sorabilecek girdiye sahip olması.
///
/// <para><b>Neden şekil testi:</b> guard bir Wolverine middleware'idir — bağımlılıklarını
/// imzasından alır. <c>IQuerySession</c> imzadan düşerse hedefin yolu okunamaz ve kod yine
/// derlenir: kapsam sessizce kimlik eşitliğine geriler, yani il yetkilisi hiçbir okulun
/// kaydını açamaz. Derleyici bunu görmez, entegrasyon testi olmadan da görülmez.</para>
/// </summary>
public sealed class InstitutionScopeGuardShapeTests
{
    private static MethodInfo GuardMethod() =>
        typeof(InstitutionScopeGuardMiddleware)
            .GetMethod("BeforeAsync", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "InstitutionScopeGuardMiddleware.BeforeAsync bulunamadı. Guard senkron "
                + "kaldıysa hedefin yolu okunamaz ve kapsam kimlik eşitliğine geriler.");

    [Fact]
    public void Guard_asenkrondur()
    {
        GuardMethod().ReturnType.ShouldBe(typeof(Task));
    }

    [Fact]
    public void Guard_hedefin_yolunu_okuyabilecek_girdiye_sahiptir()
    {
        var parameterTypes = GuardMethod().GetParameters().Select(p => p.ParameterType.Name).ToList();

        parameterTypes.ShouldContain("IInstitutionScoped");
        parameterTypes.ShouldContain("ICurrentUserService");
        parameterTypes.ShouldContain(
            "IQuerySession",
            customMessage: "Guard hedefin Path alanını okuyamıyor; il/ilçe yetkilisi kendi "
                         + "alt ağacındaki okulun kaydını açamaz.");
    }

    /// <summary>
    /// Eski senkron giriş noktası kalmamalı: Wolverine ikisini de bulursa hangisinin
    /// koşacağı belirsizleşir ve yanlışlıkla dar olan seçilebilir.
    /// </summary>
    [Fact]
    public void Eski_senkron_giris_noktasi_kalmaz()
    {
        typeof(InstitutionScopeGuardMiddleware)
            .GetMethod("Before", BindingFlags.Public | BindingFlags.Static)
            .ShouldBeNull();
    }
}
