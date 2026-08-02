using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Attendance;

/// <summary>
/// MESEM ücretli izin başvurusu uçlarının black-box davranış testleri (#177).
///
/// Kapsanan endpoint'ler:
///   PaidLeaveEndpoints (/api/attendance/paid-leave)
///     POST /api/attendance/paid-leave/                              (RequestPaidLeave)
///     POST /api/attendance/paid-leave/{requestId}/business-approve  (BusinessApprovePaidLeave)
///     POST /api/attendance/paid-leave/{requestId}/business-reject   (RejectPaidLeave — 1. adım)
///     POST /api/attendance/paid-leave/{requestId}/approve           (ApprovePaidLeave)
///     POST /api/attendance/paid-leave/{requestId}/reject            (RejectPaidLeave — 2. adım)
///     GET  /api/attendance/paid-leave/                              (ListPaidLeaveRequests)
///
/// Kural: mutasyon yapan happy-path testleri yazılmaz (paylaşılan dev DB kirlenir).
/// Buradaki testler validation-reddi ve not-found yollarının 500 dönmediğini doğrular.
/// </summary>
[Collection("api")]
public sealed class PaidLeaveApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJson() => new("{}", Encoding.UTF8, "application/json");

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Ucretli_izin_listesi_yetkili_istekle_OK_doner()
    {
        // Given — yetkili istemci; kapsam sunucuda claim'lerden çözülür
        // When — başvurular sayfalı olarak istenir
        var response = await _fixture.Client.GetAsync("/api/attendance/paid-leave/");

        // Then — boş liste de geçerli sonuçtur; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ucretli_izin_listesi_durum_filtresiyle_OK_doner()
    {
        // Given — geçerli bir durum filtresi
        // When
        var response = await _fixture.Client.GetAsync(
            "/api/attendance/paid-leave/?status=PendingSchool&page=1&pageSize=10");

        // Then
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Gecersiz_durum_filtresi_500_donmez()
    {
        // Given — SmartEnum'da karşılığı olmayan durum adı
        // When
        var response = await _fixture.Client.GetAsync(
            "/api/attendance/paid-leave/?status=OlmayanDurum");

        // Then — bilinmeyen filtre yok sayılır; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Basvuru_bos_govdeyle_500_donmez()
    {
        // Given — gerekçesiz/tarihsiz gövde
        // When
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/paid-leave/?academicPeriodId={Guid.NewGuid()}", EmptyJson());

        // Then — validation reddi (4xx) beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Başvuru <c>StudentId</c> taşımaz — sunucu onu <c>student_id</c> claim'inden alır.
    /// İstek gövdesinde öğrenci gönderilse bile bir etkisi olmamalı, 500 hiç olmamalıdır.
    /// </summary>
    [Fact]
    public async Task Basvuruda_gonderilen_ogrenci_kimligi_500_dogurmaz()
    {
        // Given — istekten öğrenci seçmeye çalışan gövde
        var body = $$"""
            {"studentId":"{{Guid.NewGuid()}}","startDate":"2030-05-10T00:00:00Z",
             "endDate":"2030-05-12T00:00:00Z","reason":"Telafi eğitimi"}
            """;

        // When
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/paid-leave/?academicPeriodId={Guid.NewGuid()}", Json(body));

        // Then — kapsam/dönem reddi (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_basvurunun_isletme_onayi_500_donmez()
    {
        // Given — var olmayan başvuru kimliği
        var requestId = Guid.NewGuid();

        // When
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/paid-leave/{requestId}/business-approve", EmptyJson());

        // Then — not-found (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_basvurunun_okul_onayi_500_donmez()
    {
        // Given
        var requestId = Guid.NewGuid();

        // When
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/paid-leave/{requestId}/approve", EmptyJson());

        // Then
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Isletme_reddi_gerekcesiz_500_donmez()
    {
        // Given — gerekçesiz ret gövdesi
        var requestId = Guid.NewGuid();

        // When
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/paid-leave/{requestId}/business-reject", EmptyJson());

        // Then — validation/not-found (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Okul_reddi_gerekcesiz_500_donmez()
    {
        // Given
        var requestId = Guid.NewGuid();

        // When
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/paid-leave/{requestId}/reject", EmptyJson());

        // Then
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kimliksiz_istek_401_doner()
    {
        // Given — token'sız istemci
        // When
        var response = await _fixture.Anonymous.GetAsync("/api/attendance/paid-leave/");

        // Then
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
