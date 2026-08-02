using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Attendance;

/// <summary>
/// Attendance modülünün tüm HTTP endpoint'leri için BDD-style black-box davranış testleri.
///
/// Kapsanan endpoint'ler:
///   AttendanceEndpoints  (/api/attendance)
///     POST   /api/attendance/                              (MarkAttendance)
///     POST   /api/attendance/{attendanceId:guid}/approve   (ApproveAttendance)
///     POST   /api/attendance/{attendanceId:guid}/verify    (VerifyAttendance)
///     POST   /api/attendance/{attendanceId:guid}/correct   (CorrectAttendance)
///     POST   /api/attendance/{attendanceId:guid}/health-report (AttachHealthReport — multipart)
///     POST   /api/attendance/{attendanceId:guid}/health-report/approve (ApproveHealthReport)
///     POST   /api/attendance/{attendanceId:guid}/health-report/reject (RejectHealthReport)
///     DELETE /api/attendance/{attendanceId:guid}           (DeleteAttendance)
///     GET    /api/attendance/{attendanceId:guid}           (GetAttendanceRecord)
///     GET    /api/attendance/                              (ListAttendanceRecords)
///   WorkCalendarEndpoints (/api/work-calendar)
///     POST   /api/work-calendar/                           (UpdateWorkCalendar)
///     GET    /api/work-calendar/                           (GetWorkCalendar)
///
/// Kural: Mutasyon yapan happy-path testleri yazılmaz (paylaşılan dev DB kirlenir).
/// Yalnızca validation-reddi, auth (401), not-found ve liste-okuma davranışları doğrulanır.
/// </summary>
[Collection("api")]
public sealed class AttendanceApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJson() => new("{}", Encoding.UTF8, "application/json");

    // ---------------------------------------------------------------------
    // GET /api/attendance/  — liste okuma
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Devamsizlik_listesi_yetkili_istekle_OK_doner()
    {
        // Given — yetkili (Bearer token'lı) bir istemci
        // When — devamsızlık kayıtları sayfalı olarak istenir (filtre yok)
        var response = await _fixture.Client.GetAsync("/api/attendance/");

        // Then — sunucu hatası DEĞİL, başarılı liste yanıtı (boş olsa bile geçerli)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Devamsizlik_listesi_filtrelerle_OK_doner()
    {
        // Given — eşleşme bulunmayacak rastgele filtre kimlikleri
        var studentId = Guid.NewGuid();
        var businessId = Guid.NewGuid();
        var institutionId = Guid.NewGuid();
        var academicPeriodId = Guid.NewGuid();

        // When — tüm opsiyonel filtreler verilerek liste istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/attendance/?studentId={studentId}&businessId={businessId}" +
            $"&institutionId={institutionId}&academicPeriodId={academicPeriodId}" +
            "&status=Pending&year=2026&month=6&page=1&pageSize=20");

        // Then — eşleşme yok = boş sayfa (geçerli), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ---------------------------------------------------------------------
    // GET /api/attendance/{attendanceId:guid}  — detay (null-return 500 bug testi)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Olmayan_devamsizlik_kaydi_istenince_500_donmez()
    {
        // Given — hiç var olmayan rastgele bir devamsızlık kimliği
        var attendanceId = Guid.NewGuid();

        // When — o kaydın detayı istenir
        var response = await _fixture.Client.GetAsync($"/api/attendance/{attendanceId}");

        // Then — kayıt yok = geçerli boş durum → 404/422 beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ---------------------------------------------------------------------
    // Auth (401) — token'sız temsilci istek
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Devamsizlik_listesi_tokensiz_istekle_401_doner()
    {
        // Given — kimlik doğrulamasız (token'sız) istemci
        // When — yetki gerektiren liste endpoint'i çağrılır
        var response = await _fixture.Anonymous.GetAsync("/api/attendance/");

        // Then — yetkilendirme reddi → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Calisma_takvimi_tokensiz_istekle_401_doner()
    {
        // Given — kimlik doğrulamasız (token'sız) istemci
        // When — yetki gerektiren çalışma takvimi endpoint'i çağrılır
        var response = await _fixture.Anonymous.GetAsync(
            $"/api/work-calendar/?institutionId={Guid.NewGuid()}&year=2026");

        // Then — yetkilendirme reddi → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ---------------------------------------------------------------------
    // POST /api/attendance/  — MarkAttendance (geçersiz gövde reddi)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Devamsizlik_olusturma_bos_govdeyle_500_donmez()
    {
        // Given — yetkili istemci ve boş/geçersiz JSON gövde
        // When — devamsızlık kaydı oluşturulmaya çalışılır
        var response = await _fixture.Client.PostAsync("/api/attendance/", EmptyJson());

        // Then — validation reddi (4xx) beklenir, sunucu hatası (500) DEĞİL; mutasyon olmaz
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ---------------------------------------------------------------------
    // POST /api/attendance/{id}/approve  — ApproveAttendance
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Olmayan_devamsizligi_onaylama_500_donmez()
    {
        // Given — var olmayan rastgele bir devamsızlık kimliği
        var attendanceId = Guid.NewGuid();

        // When — o kayıt onaylanmaya çalışılır (gövde gerekmiyor)
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/{attendanceId}/approve", EmptyJson());

        // Then — kayıt yok = 404/422 beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ---------------------------------------------------------------------
    // POST /api/attendance/{id}/verify  — VerifyAttendance
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Olmayan_devamsizligi_dogrulama_500_donmez()
    {
        // Given — var olmayan rastgele bir devamsızlık kimliği
        var attendanceId = Guid.NewGuid();

        // When — o kayıt doğrulanmaya çalışılır (gövde gerekmiyor)
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/{attendanceId}/verify", EmptyJson());

        // Then — kayıt yok = 404/422 beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ---------------------------------------------------------------------
    // POST /api/attendance/{id}/correct  — CorrectAttendance (gövdeli)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Devamsizlik_duzeltme_bos_govdeyle_500_donmez()
    {
        // Given — var olmayan kimlik ve boş/geçersiz JSON gövde
        var attendanceId = Guid.NewGuid();

        // When — düzeltme isteği gönderilir
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/{attendanceId}/correct", EmptyJson());

        // Then — validation/not-found (4xx) beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ---------------------------------------------------------------------
    // POST /api/attendance/{id}/health-report  — AttachHealthReport (multipart, #172)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Saglik_raporu_yukleme_dosyasiz_500_donmez()
    {
        // Given — var olmayan kimlik ve dosyası olmayan multipart gövde
        var attendanceId = Guid.NewGuid();
        using var form = new MultipartFormDataContent();

        // When — sağlık raporu yükleme isteği gönderilir
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/{attendanceId}/health-report", form);

        // Then — validation/not-found (4xx) beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Saglik_raporu_yukleme_json_govdeyle_500_donmez()
    {
        // Given — uç artık multipart bekliyor; eski JSON gövde 4xx vermeli, 500 değil
        var attendanceId = Guid.NewGuid();

        // When
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/{attendanceId}/health-report", EmptyJson());

        // Then
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ---------------------------------------------------------------------
    // POST /api/attendance/{id}/health-report/approve|reject  — onay zinciri (#172)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Olmayan_kaydin_saglik_raporunu_onaylama_500_donmez()
    {
        // Given — var olmayan rastgele bir devamsızlık kimliği
        var attendanceId = Guid.NewGuid();

        // When — rapor onaylanmaya çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/{attendanceId}/health-report/approve", EmptyJson());

        // Then — validation/not-found (4xx) beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Saglik_raporu_reddi_gerekcesiz_500_donmez()
    {
        // Given — var olmayan kimlik ve gerekçesiz gövde
        var attendanceId = Guid.NewGuid();

        // When — rapor reddedilmeye çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/attendance/{attendanceId}/health-report/reject", EmptyJson());

        // Then — validation/not-found (4xx) beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ---------------------------------------------------------------------
    // DELETE /api/attendance/{id}  — DeleteAttendance
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Olmayan_devamsizligi_silme_500_donmez()
    {
        // Given — var olmayan rastgele bir devamsızlık kimliği
        var attendanceId = Guid.NewGuid();

        // When — o kayıt silinmeye çalışılır
        var response = await _fixture.Client.DeleteAsync($"/api/attendance/{attendanceId}");

        // Then — kayıt yok = 404/422 beklenir, sunucu hatası (500) DEĞİL; mutasyon olmaz
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ---------------------------------------------------------------------
    // POST /api/work-calendar/  — UpdateWorkCalendar (gövdeli)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Calisma_takvimi_guncelleme_bos_govdeyle_500_donmez()
    {
        // Given — yetkili istemci ve boş/geçersiz JSON gövde
        // When — çalışma takvimi güncellenmeye çalışılır
        var response = await _fixture.Client.PostAsync("/api/work-calendar/", EmptyJson());

        // Then — validation reddi (4xx) beklenir, sunucu hatası (500) DEĞİL; mutasyon olmaz
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ---------------------------------------------------------------------
    // GET /api/work-calendar/  — GetWorkCalendar (institutionId + year zorunlu query)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Olmayan_kurumun_calisma_takvimi_istenince_500_donmez()
    {
        // Given — çalışma takvimi olmayan rastgele bir kurum ve yıl
        var institutionId = Guid.NewGuid();

        // When — o kurum/yıl için çalışma takvimi istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/work-calendar/?institutionId={institutionId}&year=2026");

        // Then — takvim yok = geçerli boş durum → 404/422 beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }
}
