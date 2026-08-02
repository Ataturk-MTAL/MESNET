using MESNET.Common.Shared.Security;
using MESNET.Coordination.Api;
using MESNET.Coordination.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Dönem notu uçlarının izin kilidi (#171).
///
/// <para>Kilitlenen karar: <b>iki ayrı akış, iki ayrı izin.</b> İşletmede staj notunu işletme
/// girer (<c>company:grade:enter</c>, kapsam <c>business_id</c> claim'i); okulda staj notunu
/// okul girer (<c>institution:school-grade:enter</c>, kapsam <c>institution_id</c>). Okuldaki
/// şefin <c>business_id</c> claim'i yoktur — işletme izni ona hiçbir işe yaramazdı.</para>
///
/// <para>Regresyon koruması: okul izni işletme uçlarına, işletme izni okul uçlarına
/// sızmamalı. Sızsaydı ya işletme okulda staj notunu girer ya da okul işletmenin gireceği
/// notu onun yerine yazardı.</para>
/// </summary>
public sealed class SchoolTermGradeEndpointAuthorizationTests
{
    private const string BusinessEnterRoute = "/api/coordination/term-grades/";
    private const string BusinessStudentsRoute = "/api/coordination/term-grades/my-students";
    private const string SchoolEnterRoute = "/api/coordination/term-grades/school";
    private const string SchoolStudentsRoute = "/api/coordination/term-grades/school-students";
    private const string SchoolSubmitRoute = "/api/coordination/term-grades/school/{id:guid}/submit";

    [Theory]
    [InlineData(SchoolEnterRoute)]
    [InlineData(SchoolSubmitRoute)]
    public void Okulda_staj_yazma_uclari_okul_iznini_ister(string route)
    {
        PoliciesOf(HttpMethods.Post, route)
            .ShouldContain(Permissions.Institution.SchoolGradeEnter);
    }

    [Fact]
    public void Okulda_staj_ogrenci_listesi_okul_iznini_ister()
    {
        PoliciesOf(HttpMethods.Get, SchoolStudentsRoute)
            .ShouldContain(Permissions.Institution.SchoolGradeEnter);
    }

    /// <summary>
    /// Okul uçları işletme iznini KABUL ETMEZ. Etseydi işletme yetkilisi okulda staj yapan
    /// öğrencinin notunu girebilirdi — o öğrencinin işvereni yok.
    /// </summary>
    [Theory]
    // HttpMethods.Post/Get static readonly'dır, sabit değil — attribute'ta düz metin kullanılır.
    [InlineData("POST", SchoolEnterRoute)]
    [InlineData("POST", SchoolSubmitRoute)]
    [InlineData("GET", SchoolStudentsRoute)]
    public void Okul_uclari_isletme_iznini_kabul_etmez(string method, string route)
    {
        PoliciesOf(method, route).ShouldNotContain(Permissions.Company.EnterGrade);
    }

    /// <summary>
    /// <b>Regresyon:</b> işletme akışı değişmedi — uçları hâlâ yalnız işletme iznini ister.
    /// </summary>
    [Fact]
    public void Isletme_uclari_degismedi()
    {
        var enterPolicies = PoliciesOf(HttpMethods.Post, BusinessEnterRoute);
        enterPolicies.ShouldContain(Permissions.Company.EnterGrade);
        enterPolicies.ShouldNotContain(Permissions.Institution.SchoolGradeEnter);

        var listPolicies = PoliciesOf(HttpMethods.Get, BusinessStudentsRoute);
        listPolicies.ShouldContain(Permissions.Company.EnterGrade);
        listPolicies.ShouldNotContain(Permissions.Institution.SchoolGradeEnter);
    }

    /// <summary>
    /// Okul izni <b>yalnız</b> okulda staj uçlarında olmalı. Başka uca yayan biri, o ucu
    /// sessizce alan şefine ve müdür yardımcısına da açmış olur — ikisi de bu izni açık
    /// satırla taşıyor.
    /// </summary>
    [Fact]
    public void Okul_izni_baska_hicbir_uca_uygulanmaz()
    {
        var carriers = TermGradeEndpoints()
            .Where(e => e.Policies.Contains(Permissions.Institution.SchoolGradeEnter))
            .Select(e => $"{e.Method} {e.Route}")
            .OrderBy(x => x)
            .ToList();

        carriers.ShouldBe(
        [
            $"{HttpMethods.Get} {SchoolStudentsRoute}",
            $"{HttpMethods.Post} {SchoolEnterRoute}",
            $"{HttpMethods.Post} {SchoolSubmitRoute}",
        ], ignoreOrder: true);
    }

    // ── Yardımcılar ──

    private static IReadOnlyList<string> PoliciesOf(string method, string route) =>
        EndpointPolicyProbe.PoliciesOf(TermGradeEndpoints(), method, route);

    private static IReadOnlyList<EndpointPolicyProbe.EndpointInfo> TermGradeEndpoints() =>
        EndpointPolicyProbe.Collect(builder => builder.MapStudentTermGradeEndpoints());
}
