using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Reporting;

/// <summary>
/// Reporting modülü HTTP endpoint'leri için BDD-style black-box davranış testleri.
/// Kapsam: ReportingEndpoints (/api/reports) + DocumentLifecycleEndpoints (/api/reports/documents).
///
/// Kurallar:
/// - Happy-path CREATE/UPDATE/DELETE YOK (paylaşılan dev DB'yi kirletmemek için).
/// - Sadece auth (401), not-found/geçersiz id (500 DEĞİL), validation reddi (4xx, 500 DEĞİL) ve liste okuma.
/// - POST/PUT mutasyonları boş/geçersiz gövdeyle gönderilir → reddedilir, veri yazılmaz.
/// </summary>
[Collection("api")]
public sealed class ReportingApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJson() =>
        new("{}", Encoding.UTF8, "application/json");

    // =====================================================================
    // Auth (401) — temsilci endpoint'ler token'sız erişilemez
    // =====================================================================

    [Fact]
    public async Task Token_olmadan_dokuman_listesi_istenince_401_doner()
    {
        // Given — kimlik doğrulaması olmayan istemci
        // When — doküman listesi token'sız istenir
        var response = await _fixture.Anonymous.GetAsync("/api/reports/documents/");

        // Then — yetkisiz erişim reddedilir
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_olmadan_staj_sozlesmesi_olusturulunca_401_doner()
    {
        // Given — kimlik doğrulaması olmayan istemci
        // When — staj sözleşmesi üretim isteği token'sız gönderilir
        var response = await _fixture.Anonymous.PostAsync(
            "/api/reports/internship-contract", EmptyJson());

        // Then — yetkisiz erişim reddedilir
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_olmadan_dokuman_detayi_istenince_401_doner()
    {
        // Given — kimlik doğrulaması olmayan istemci ve rastgele bir doküman id'si
        var documentId = Guid.NewGuid();

        // When — doküman detayı token'sız istenir
        var response = await _fixture.Anonymous.GetAsync(
            $"/api/reports/documents/{documentId}");

        // Then — yetkisiz erişim reddedilir
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_olmadan_aylik_devamsizlik_onizlemesi_istenince_401_doner()
    {
        // Given — kimlik doğrulaması olmayan istemci
        // When — Form 7 aylık devamsızlık önizleme PDF'i token'sız istenir
        var response = await _fixture.Anonymous.GetAsync(
            "/api/reports/monthly-attendance/preview" +
            $"?institutionId={Guid.NewGuid()}&academicPeriodId={Guid.NewGuid()}" +
            $"&businessId={Guid.NewGuid()}&year=2026&month=6" +
            "&institutionName=Test&academicYear=2025-2026");

        // Then — yetkisiz erişim reddedilir
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_olmadan_dokuman_silinince_401_doner()
    {
        // Given — kimlik doğrulaması olmayan istemci ve rastgele bir doküman id'si
        var documentId = Guid.NewGuid();

        // When — doküman silme isteği token'sız gönderilir
        var response = await _fixture.Anonymous.DeleteAsync(
            $"/api/reports/documents/{documentId}");

        // Then — yetkisiz erişim reddedilir
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // =====================================================================
    // DocumentLifecycle — GET detay/PDF (null-return 500 bug'ını yakalar)
    // =====================================================================

    [Fact]
    public async Task Var_olmayan_dokumanin_detayi_istenince_500_donmez()
    {
        // Given — hiç oluşturulmamış rastgele bir doküman id'si
        var documentId = Guid.NewGuid();

        // When — o doküman için detay istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/reports/documents/{documentId}");

        // Then — 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_dokumanin_pdfi_istenince_500_donmez()
    {
        // Given — hiç oluşturulmamış rastgele bir doküman id'si
        var documentId = Guid.NewGuid();

        // When — o doküman için PDF indirme isteği yapılır
        var response = await _fixture.Client.GetAsync(
            $"/api/reports/documents/{documentId}/pdf");

        // Then — 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // =====================================================================
    // DocumentLifecycle — GET liste (filtreli + sayfalı)
    // =====================================================================

    [Fact]
    public async Task Dokuman_listesi_istenince_basariyla_doner()
    {
        // Given — yetkili istemci
        // When — sayfalı doküman listesi istenir
        var response = await _fixture.Client.GetAsync(
            "/api/reports/documents/?page=1&pageSize=20");

        // Then — başarılı yanıt döner, sunucu hatası olmaz
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Dokuman_listesi_yaniti_sayfali_zarf_doner()
    {
        // Given — yetkili istemci
        // When — sayfalı doküman listesi istenir
        var response = await _fixture.Client.GetAsync(
            "/api/reports/documents/?page=1&pageSize=20");

        // Then — yanıt başarılı olmalı ve data zarfı sayfalama alanlarını içermeli
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var data = await ApiTestFixture.ReadDataAsync(response);
        data.TryGetProperty("items", out _).ShouldBeTrue();
        data.TryGetProperty("totalCount", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Dokuman_listesi_filtrelerle_istenince_500_donmez()
    {
        // Given — durum/form tipi/öğretmen/kurum filtreleri ile bir sorgu
        var teacherId = Guid.NewGuid();
        var institutionId = Guid.NewGuid();

        // When — filtreli ve aramalı doküman listesi istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/reports/documents/?status=Pending&formType=InternshipContract" +
            $"&teacherId={teacherId}&institutionId={institutionId}" +
            $"&page=1&pageSize=10&sortBy=createdAt&descending=true&search=test");

        // Then — boş sonuç geçerlidir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ogrenciye_ait_dokumanlar_istenince_500_donmez()
    {
        // Given — hiç dokümanı olmayan rastgele bir öğrenci id'si
        var studentId = Guid.NewGuid();

        // When — o öğrencinin dokümanları istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/reports/documents/by-student/{studentId}");

        // Then — boş liste geçerlidir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // =====================================================================
    // DocumentLifecycle — POST durum geçişleri (var olmayan id → 500 değil)
    // =====================================================================

    [Fact]
    public async Task Var_olmayan_dokuman_yazdirildi_isaretlenince_500_donmez()
    {
        // Given — hiç oluşturulmamış rastgele bir doküman id'si
        var documentId = Guid.NewGuid();

        // When — doküman "yazdırıldı" olarak işaretlenmeye çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/reports/documents/{documentId}/print", EmptyJson());

        // Then — 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_dokuman_imzalanip_teslim_edildi_isaretlenince_500_donmez()
    {
        // Given — hiç oluşturulmamış rastgele bir doküman id'si
        var documentId = Guid.NewGuid();

        // When — doküman "imzalanıp teslim edildi" olarak işaretlenmeye çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/reports/documents/{documentId}/sign-and-return", EmptyJson());

        // Then — 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_dokuman_arsivlenince_500_donmez()
    {
        // Given — hiç oluşturulmamış rastgele bir doküman id'si
        var documentId = Guid.NewGuid();

        // When — doküman arşivlenmeye çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/reports/documents/{documentId}/archive", EmptyJson());

        // Then — 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_dokuman_silinince_500_donmez()
    {
        // Given — hiç oluşturulmamış rastgele bir doküman id'si
        var documentId = Guid.NewGuid();

        // When — doküman silinmeye çalışılır
        var response = await _fixture.Client.DeleteAsync(
            $"/api/reports/documents/{documentId}");

        // Then — 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // =====================================================================
    // DocumentLifecycle — POST body'li (boş/geçersiz gövde → 4xx, mutasyon yok)
    // =====================================================================

    [Fact]
    public async Task Bos_govdeyle_zip_indirme_istenince_500_donmez()
    {
        // Given — geçersiz (boş) ZIP indirme isteği gövdesi
        // When — boş gövdeyle ZIP indirme istenir
        var response = await _fixture.Client.PostAsync(
            "/api/reports/documents/download-zip", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bos_govdeyle_toplu_belge_uretimi_istenince_500_donmez()
    {
        // Given — geçersiz (boş) toplu belge üretim isteği gövdesi
        // When — boş gövdeyle toplu belge üretimi istenir (mutasyon reddedilir)
        var response = await _fixture.Client.PostAsync(
            "/api/reports/documents/generate-batch", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bos_govdeyle_toplu_silme_istenince_500_donmez()
    {
        // Given — geçersiz (boş) toplu silme isteği gövdesi
        // When — boş gövdeyle toplu silme istenir (mutasyon reddedilir)
        var response = await _fixture.Client.PostAsync(
            "/api/reports/documents/batch-delete", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // =====================================================================
    // Reporting — POST form üretimleri (boş gövde → 4xx, mutasyon yok)
    // =====================================================================

    [Fact]
    public async Task Bos_govdeyle_staj_sozlesmesi_uretilince_500_donmez()
    {
        // Given — geçersiz (boş) staj sözleşmesi form verisi
        // When — boş gövdeyle staj sözleşmesi üretimi istenir
        var response = await _fixture.Client.PostAsync(
            "/api/reports/internship-contract", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bos_govdeyle_aylik_egitim_faaliyeti_uretilince_500_donmez()
    {
        // Given — geçersiz (boş) aylık eğitim faaliyeti form verisi
        // When — boş gövdeyle form üretimi istenir
        var response = await _fixture.Client.PostAsync(
            "/api/reports/monthly-activity", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bos_govdeyle_gunluk_rehberlik_formu_uretilince_500_donmez()
    {
        // Given — geçersiz (boş) günlük rehberlik form verisi
        // When — boş gövdeyle form üretimi istenir
        var response = await _fixture.Client.PostAsync(
            "/api/reports/guidance-visit", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bos_govdeyle_devamsizlik_cizelgesi_uretilince_500_donmez()
    {
        // Given — geçersiz (boş) devamsızlık çizelgesi form verisi
        // When — boş gövdeyle form üretimi istenir
        var response = await _fixture.Client.PostAsync(
            "/api/reports/attendance-sheet", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bos_govdeyle_beceri_sinavi_not_fisi_uretilince_500_donmez()
    {
        // Given — geçersiz (boş) beceri sınavı not fişi form verisi
        // When — boş gövdeyle form üretimi istenir
        var response = await _fixture.Client.PostAsync(
            "/api/reports/skill-exam", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bos_govdeyle_isletme_degerlendirme_formu_uretilince_500_donmez()
    {
        // Given — geçersiz (boş) işletme değerlendirme form verisi
        // When — boş gövdeyle form üretimi istenir
        var response = await _fixture.Client.PostAsync(
            "/api/reports/business-evaluation", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bos_govdeyle_aylik_devamsizlik_arsiv_belgesi_uretilince_500_donmez()
    {
        // Given — geçersiz (boş) aylık devamsızlık (arşiv) form verisi
        // When — boş gövdeyle form üretimi istenir
        var response = await _fixture.Client.PostAsync(
            "/api/reports/monthly-attendance", EmptyJson());

        // Then — validation/bad request beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // =====================================================================
    // Reporting — GET Form 7 önizleme (zorunlu query parametreleri ile)
    // =====================================================================

    [Fact]
    public async Task Veri_olmayan_donem_icin_aylik_devamsizlik_onizlemesi_istenince_500_donmez()
    {
        // Given — hiç devamsızlık verisi olmayan rastgele kurum/dönem/işletme
        var institutionId = Guid.NewGuid();
        var academicPeriodId = Guid.NewGuid();
        var businessId = Guid.NewGuid();

        // When — aylık devamsızlık önizleme PDF'i istenir (tüm zorunlu query paramları verilir)
        var response = await _fixture.Client.GetAsync(
            "/api/reports/monthly-attendance/preview" +
            $"?institutionId={institutionId}&academicPeriodId={academicPeriodId}" +
            $"&businessId={businessId}&year=2026&month=6" +
            "&institutionName=Test%20MTAL&academicYear=2025-2026");

        // Then — boş veri geçerlidir (404/422/boş PDF), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Veri_olmayan_donem_icin_toplu_aylik_devamsizlik_onizlemesi_istenince_500_donmez()
    {
        // Given — hiç devamsızlık verisi olmayan rastgele kurum/dönem
        var institutionId = Guid.NewGuid();
        var academicPeriodId = Guid.NewGuid();

        // When — tüm öğretmenler için toplu önizleme PDF'i istenir (zorunlu query paramları)
        var response = await _fixture.Client.GetAsync(
            "/api/reports/monthly-attendance/preview-batch" +
            $"?institutionId={institutionId}&academicPeriodId={academicPeriodId}" +
            "&year=2026&month=6&institutionName=Test%20MTAL&academicYear=2025-2026");

        // Then — boş veri geçerlidir (404/422/boş PDF), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }
}
