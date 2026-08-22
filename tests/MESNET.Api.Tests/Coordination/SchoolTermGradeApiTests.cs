using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Coordination;

/// <summary>
/// Okulda staj dönem notu uçlarının black-box davranış testleri (#171).
///
/// Kapsanan endpoint'ler:
///   StudentTermGradeEndpoints (/api/coordination/term-grades)
///     GET  /school-students             (GetSchoolStudentsForGrading)
///     POST /school                      (EnterSchoolTermGrade)
///     POST /school/{id}/submit          (SubmitSchoolTermGrade)
///
/// Kural: mutasyon yapan happy-path testleri yazılmaz (paylaşılan dev DB kirlenir).
/// Buradaki testler validation/kapsam reddi yollarının 500 dönmediğini doğrular.
/// </summary>
[Collection("api")]
public sealed class SchoolTermGradeApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJson() => new("{}", Encoding.UTF8, "application/json");

    [Fact]
    public async Task Okulda_staj_ogrenci_listesi_yetkili_istekle_OK_doner()
    {
        // Given — yetkili istemci; kapsam institution_id claim'inden çözülür
        // When
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/term-grades/school-students?academicPeriodId={Guid.NewGuid()}");

        // Then — boş liste de geçerli sonuçtur; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Okulda_staj_notu_bos_govdeyle_500_donmez()
    {
        // Given — öğrencisiz/dönemsiz gövde
        // When
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/term-grades/school", EmptyJson());

        // Then — validation reddi (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Okulda staj yerleştirmesi olmayan öğrenci için giriş REDDEDİLİR — işletmede staj yapan
    /// öğrencinin notunu okul bu yoldan yazamaz.
    /// </summary>
    [Fact]
    public async Task Yerlestirmesi_olmayan_ogrenci_icin_giris_500_donmez()
    {
        // Given — rastgele (yerleştirmesi olmayan) öğrenci ve dönem
        var body = $$"""
            {"studentId":"{{Guid.NewGuid()}}","academicPeriodId":"{{Guid.NewGuid()}}",
             "practiceGrades":[85],"serviceGrades":[],"projectGrades":[],"experimentGrades":[]}
            """;

        // When
        var response = await _fixture.Client.PostAsync(
            "/api/coordination/term-grades/school",
            new StringContent(body, Encoding.UTF8, "application/json"));

        // Then — kapsam/pencere reddi (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_notun_gonderimi_500_donmez()
    {
        // Given — var olmayan not kaydı
        var gradeId = Guid.NewGuid();

        // When
        var response = await _fixture.Client.PostAsync(
            $"/api/coordination/term-grades/school/{gradeId}/submit", EmptyJson());

        // Then — not-found (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kimliksiz_istek_401_doner()
    {
        // Given — token'sız istemci
        // When
        var response = await _fixture.Anonymous.GetAsync(
            $"/api/coordination/term-grades/school-students?academicPeriodId={Guid.NewGuid()}");

        // Then
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// <b>Regresyon (#171):</b> işletme akışı değişmedi — kendi öğrenci listesi hâlâ çalışıyor.
    /// </summary>
    [Fact]
    public async Task Isletme_ogrenci_listesi_calismaya_devam_eder()
    {
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/term-grades/my-students?academicPeriodId={Guid.NewGuid()}");

        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
