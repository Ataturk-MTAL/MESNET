using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Contract;

/// <summary>
/// Contract modülünün (/api/contracts) tüm HTTP endpoint'leri için BDD-style black-box davranış testleri.
///
/// Kapsanan endpoint'ler (ContractEndpoints — grup prefix'i /api/contracts):
///   POST   /api/contracts/                                  (CreateContract)
///   POST   /api/contracts/{contractId:guid}/submit          (SubmitContractForSignature)
///   POST   /api/contracts/{contractId:guid}/sign            (SignContract)
///   POST   /api/contracts/{contractId:guid}/activate        (ActivateContract)
///   POST   /api/contracts/{contractId:guid}/suspend         (SuspendContract)
///   POST   /api/contracts/{contractId:guid}/resume          (ResumeContract)
///   POST   /api/contracts/{contractId:guid}/terminate       (TerminateContract)
///   POST   /api/contracts/{contractId:guid}/complete        (CompleteContract)
///   POST   /api/contracts/{contractId:guid}/request-termination (RequestTermination)
///   POST   /api/contracts/{contractId:guid}/reject-termination  (RejectTermination)
///   GET    /api/contracts/{contractId:guid}                 (GetContract)
///   GET    /api/contracts/                                  (ListContracts — sayfalı)
///   POST   /api/contracts/{contractId:guid}/documents       (UploadContractDocument — multipart form)
///
/// Test felsefesi (BDD-style: Given/When/Then):
/// - Hiçbir test paylaşılan dev DB'sini KİRLETMEZ: yalnızca validation-reddi, auth (401),
///   not-found (404/422) ve liste-okuma senaryoları çalıştırılır. Happy-path
///   CREATE/UPDATE/DELETE YAPILMAZ.
/// - Var olmayan kimliklerle yapılan istekler 404/422 dönmelidir, ASLA 500 (sunucu hatası)
///   dönmemelidir. Bu testler özellikle "null-return → 500" bug'ını yakalar.
/// </summary>
[Collection("api")]
public sealed class ContractApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJson() =>
        new("{}", Encoding.UTF8, "application/json");

    // ────────────────────────────────────────────────────────────────────────
    // GET /api/contracts — Sözleşme listesi (sayfalı)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sozlesme_listesi_yetkili_istekte_OK_doner()
    {
        // Given — yetkili (Bearer token'lı) bir kullanıcı
        // When — sözleşme listesi istenir (zorunlu filtre yok, hepsi opsiyonel)
        var response = await _fixture.Client.GetAsync("/api/contracts");

        // Then — liste okuması başarılı olmalı, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sozlesme_listesi_filtre_ve_sayfalama_parametreleriyle_OK_doner()
    {
        // Given — yetkili kullanıcı ve rastgele filtre/sayfalama parametreleri
        var studentId = Guid.NewGuid();
        var academicPeriodId = Guid.NewGuid();

        // When — filtreli + sayfalı liste istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/contracts?studentId={studentId}&academicPeriodId={academicPeriodId}" +
            "&status=Active&page=1&pageSize=10&sortBy=createdAt&descending=true&search=test");

        // Then — eşleşme olmasa bile boş sayfalı sonuç döner, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ────────────────────────────────────────────────────────────────────────
    // GET /api/contracts/{contractId:guid} — Sözleşme detayı
    // EN ÖNEMLİ TEST: var olmayan kimlik 404 döndürmeli, null-return → 500 OLMAMALI
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesme_detayi_istenince_404_doner_500_degil()
    {
        // Given — sistemde olmayan rastgele bir sözleşme kimliği
        var contractId = Guid.NewGuid();

        // When — o sözleşmenin detayı istenir
        var response = await _fixture.Client.GetAsync($"/api/contracts/{contractId}");

        // Then — bulunamadı = geçerli durum → 404 (NotFound), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Auth — token'sız istek 401 dönmeli
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Token_olmadan_sozlesme_listesi_istenince_401_doner()
    {
        // Given — kimlik doğrulaması yapılmamış (token'sız) bir istemci
        // When — auth gerektiren liste endpoint'ine istek atılır
        var response = await _fixture.Anonymous.GetAsync("/api/contracts");

        // Then — yetkisiz erişim reddedilir → 401 (Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_olmadan_sozlesme_detayi_istenince_401_doner()
    {
        // Given — token'sız istemci ve rastgele bir sözleşme kimliği
        var contractId = Guid.NewGuid();

        // When — auth gerektiren detay endpoint'ine istek atılır
        var response = await _fixture.Anonymous.GetAsync($"/api/contracts/{contractId}");

        // Then — yetkisiz erişim reddedilir → 401 (Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_olmadan_sozlesme_olusturma_istenince_401_doner()
    {
        // Given — token'sız istemci
        // When — auth gerektiren yazma (create) endpoint'ine istek atılır
        var response = await _fixture.Anonymous.PostAsync("/api/contracts/", EmptyJson());

        // Then — yetkisiz erişim reddedilir → 401 (Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts — Sözleşme oluştur (body'li)
    // Boş gövde → validation reddi (4xx), MUTASYON YAPMAZ
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sozlesme_olusturma_bos_govdeyle_4xx_doner_500_degil()
    {
        // Given — yetkili kullanıcı ama geçersiz/boş bir create gövdesi
        // When — boş JSON gövdeyle sözleşme oluşturma denenir
        var response = await _fixture.Client.PostAsync("/api/contracts/", EmptyJson());

        // Then — validation/business-rule reddi (4xx) beklenir, sunucu hatası DEĞİL
        // (eksik zorunlu alanlar → 400/422; gerçek sözleşme OLUŞTURULMAZ)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/submit — İmzaya gönder (body'siz)
    // Var olmayan sözleşme → 4xx, 500 OLMAMALI
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesmeyi_imzaya_gonderince_4xx_doner_500_degil()
    {
        // Given — sistemde olmayan rastgele bir sözleşme kimliği
        var contractId = Guid.NewGuid();

        // When — o sözleşme imzaya gönderilmeye çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/submit", EmptyJson());

        // Then — bulunamadı/geçersiz durum → 4xx, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/sign — İmzala (body'li)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesmeyi_imzalayinca_4xx_doner_500_degil()
    {
        // Given — olmayan sözleşme kimliği ve boş imza gövdesi
        var contractId = Guid.NewGuid();

        // When — sözleşme imzalanmaya çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/sign", EmptyJson());

        // Then — validation/not-found reddi (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/activate — Aktifleştir (body'siz)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesmeyi_aktiflestirince_4xx_doner_500_degil()
    {
        // Given — olmayan sözleşme kimliği
        var contractId = Guid.NewGuid();

        // When — sözleşme aktifleştirilmeye çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/activate", EmptyJson());

        // Then — not-found/geçersiz durum reddi (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/suspend — Askıya al (body'li)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesmeyi_askiya_alinca_4xx_doner_500_degil()
    {
        // Given — olmayan sözleşme kimliği ve boş gövde
        var contractId = Guid.NewGuid();

        // When — sözleşme askıya alınmaya çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/suspend", EmptyJson());

        // Then — validation/not-found reddi (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/resume — Devam ettir (body'siz)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesmeyi_devam_ettirince_4xx_doner_500_degil()
    {
        // Given — olmayan sözleşme kimliği
        var contractId = Guid.NewGuid();

        // When — sözleşme devam ettirilmeye çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/resume", EmptyJson());

        // Then — not-found/geçersiz durum reddi (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/terminate — Feshet (body'li)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesmeyi_feshedince_4xx_doner_500_degil()
    {
        // Given — olmayan sözleşme kimliği ve boş fesih gövdesi
        var contractId = Guid.NewGuid();

        // When — sözleşme feshedilmeye çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/terminate", EmptyJson());

        // Then — validation/not-found reddi (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/complete — Tamamla (body'siz)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesmeyi_tamamlayinca_4xx_doner_500_degil()
    {
        // Given — olmayan sözleşme kimliği
        var contractId = Guid.NewGuid();

        // When — sözleşme tamamlanmaya çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/complete", EmptyJson());

        // Then — not-found/geçersiz durum reddi (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/request-termination — Fesih talebi (body'li)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesme_icin_fesih_talebi_olusturunca_4xx_doner_500_degil()
    {
        // Given — olmayan sözleşme kimliği ve boş talep gövdesi
        var contractId = Guid.NewGuid();

        // When — işletme fesih talebi oluşturmaya çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/request-termination", EmptyJson());

        // Then — validation/not-found reddi (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/reject-termination — Fesih talebini reddet (body'li)
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_sozlesme_icin_fesih_talebini_reddedince_4xx_doner_500_degil()
    {
        // Given — olmayan sözleşme kimliği ve boş gövde
        var contractId = Guid.NewGuid();

        // When — fesih talebi reddedilmeye çalışılır
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/reject-termination", EmptyJson());

        // Then — validation/not-found reddi (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ────────────────────────────────────────────────────────────────────────
    // POST /api/contracts/{contractId:guid}/documents — Evrak yükle (multipart form)
    // Form-data değil/eksik alan → 400 (BadRequest), MUTASYON YAPMAZ
    // ────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Evrak_yukleme_form_data_olmadan_400_doner_500_degil()
    {
        // Given — yetkili kullanıcı ama multipart form-data yerine JSON gövde
        var contractId = Guid.NewGuid();

        // When — yanlış content-type ile evrak yükleme denenir
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/documents", EmptyJson());

        // Then — endpoint "Multipart form-data bekleniyor" → 400, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Evrak_yukleme_eksik_form_alanlariyla_400_doner_500_degil()
    {
        // Given — yetkili kullanıcı ve zorunlu alanları (UploadedBy/DocumentType/DocumentFile) eksik bir form
        var contractId = Guid.NewGuid();
        using var form = new MultipartFormDataContent
        {
            { new StringContent(string.Empty), "Description" }
        };

        // When — eksik form-data ile evrak yükleme denenir
        var response = await _fixture.Client.PostAsync(
            $"/api/contracts/{contractId}/documents", form);

        // Then — eksik zorunlu alan → 400 (BadRequest), sunucu hatası DEĞİL; evrak YÜKLENMEZ
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
