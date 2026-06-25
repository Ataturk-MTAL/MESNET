using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Business;

/// <summary>
/// Business modülünün tüm HTTP endpoint'leri için black-box davranış testleri.
/// Mutasyon yapılmaz: yalnızca liste-okuma, not-found, auth ve validation-reddi senaryoları.
/// "Kayıt yok" geçerli bir durumdur (404/422), sunucu hatası (500) DEĞİL.
/// </summary>
[Collection("api")]
public sealed class BusinessApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJson() => new("{}", Encoding.UTF8, "application/json");

    // ─────────────────────────────────────────────────────────────────────────
    // BusinessEndpoints — /api/businesses
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_isletme_detayi_istenince_500_donmez()
    {
        // Given — kaydı olmayan rastgele bir işletme kimliği
        var businessId = Guid.NewGuid();

        // When — işletme detayı istenir
        var response = await _fixture.Client.GetAsync($"/api/businesses/{businessId}");

        // Then — bulunamadı = geçerli durum (404/422), sunucu hatası DEĞİL (null-return 500 bug'ını yakalar)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Isletme_kayit_endpointi_bos_govdeyle_500_donmez()
    {
        // Given — eksik/geçersiz bir kayıt gövdesi
        // When — POST /api/businesses çağrılır
        var response = await _fixture.Client.PostAsync("/api/businesses/", EmptyJson());

        // Then — validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Self_register_endpointi_bos_govdeyle_500_donmez()
    {
        // Given — eksik/geçersiz bir self-register gövdesi
        // When — POST /api/businesses/self-register çağrılır
        var response = await _fixture.Client.PostAsync("/api/businesses/self-register", EmptyJson());

        // Then — validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Isletme_guncelleme_bos_govdeyle_500_donmez()
    {
        // Given — var olmayan işletme + eksik gövde
        var businessId = Guid.NewGuid();

        // When — PATCH /api/businesses/{id} çağrılır
        var response = await _fixture.Client.PatchAsync($"/api/businesses/{businessId}", EmptyJson());

        // Then — validation/not-found reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_isletme_onaylanmaya_calisilinca_500_donmez()
    {
        // Given — var olmayan bir işletme
        var businessId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/approve çağrılır (gövdesiz)
        var response = await _fixture.Client.PostAsync($"/api/businesses/{businessId}/approve", null);

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_isletme_reddedilmeye_calisilinca_500_donmez()
    {
        // Given — var olmayan bir işletme + boş gövde
        var businessId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/reject çağrılır
        var response = await _fixture.Client.PostAsync($"/api/businesses/{businessId}/reject", EmptyJson());

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_isletme_pasife_alinmaya_calisilinca_500_donmez()
    {
        // Given — var olmayan bir işletme + boş gövde
        var businessId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/deactivate çağrılır
        var response = await _fixture.Client.PostAsync($"/api/businesses/{businessId}/deactivate", EmptyJson());

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_isletme_aktiflestirilmeye_calisilinca_500_donmez()
    {
        // Given — var olmayan bir işletme
        var businessId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/activate çağrılır (gövdesiz)
        var response = await _fixture.Client.PostAsync($"/api/businesses/{businessId}/activate", null);

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_isletme_kapatilmaya_calisilinca_500_donmez()
    {
        // Given — var olmayan bir işletme
        var businessId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/close çağrılır (gövdesiz)
        var response = await _fixture.Client.PostAsync($"/api/businesses/{businessId}/close", null);

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BusinessQueryEndpoints — /api/businesses (liste & sorgular)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Isletme_listesi_sayfali_olarak_basariyla_doner()
    {
        // Given — yetkili admin client
        // When — GET /api/businesses sayfalama parametreleriyle çağrılır
        var response = await _fixture.Client.GetAsync("/api/businesses/?page=1&pageSize=20");

        // Then — listeleme başarılı olmalı (200), her halükarda sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sektor_listesi_basariyla_doner()
    {
        // Given — yetkili admin client
        // When — GET /api/businesses/sectors çağrılır (DB'ye dokunmaz, SmartEnum listesi)
        var response = await _fixture.Client.GetAsync("/api/businesses/sectors");

        // Then — sektör listesi başarıyla dönmeli (200)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Yakindaki_isletmeler_sorgusu_500_donmez()
    {
        // Given — makul koordinat ve yarıçap değerleri (Ankara civarı, 5km)
        // When — GET /api/businesses/nearby çağrılır
        var response = await _fixture.Client.GetAsync(
            "/api/businesses/nearby?lat=39.9334&lng=32.8597&radius=5000");

        // Then — spatial sorgu başarılı olmalı, en azından sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_isletme_kapasitesi_guncellenmeye_calisilinca_500_donmez()
    {
        // Given — var olmayan bir işletme + boş/geçersiz gövde
        var businessId = Guid.NewGuid();

        // When — PUT /api/businesses/{id}/capacity çağrılır
        var response = await _fixture.Client.PutAsync($"/api/businesses/{businessId}/capacity", EmptyJson());

        // Then — validation/not-found reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BusinessDocumentEndpoints — /api/businesses/{businessId}/documents
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Belge_yukleme_dosyasiz_form_ile_500_donmez()
    {
        // Given — var olmayan işletme + dosyasız (eksik) multipart form
        var businessId = Guid.NewGuid();
        using var form = new MultipartFormDataContent
        {
            { new StringContent("Contract"), "type" },
        };

        // When — POST /api/businesses/{id}/documents çağrılır (dosya eksik)
        var response = await _fixture.Client.PostAsync($"/api/businesses/{businessId}/documents/", form);

        // Then — bad request/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_belge_onaylanmaya_calisilinca_500_donmez()
    {
        // Given — var olmayan işletme ve belge kimlikleri
        var businessId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/documents/{docId}/approve çağrılır
        var response = await _fixture.Client.PostAsync(
            $"/api/businesses/{businessId}/documents/{documentId}/approve", null);

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_belgenin_indirme_baglantisi_istenince_500_donmez()
    {
        // Given — var olmayan işletme ve belge kimlikleri
        var businessId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        // When — GET /api/businesses/{id}/documents/{docId}/url çağrılır
        var response = await _fixture.Client.GetAsync(
            $"/api/businesses/{businessId}/documents/{documentId}/url");

        // Then — bulunamadı = geçerli durum (404/422), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_belge_silinmeye_calisilinca_500_donmez()
    {
        // Given — var olmayan işletme ve belge kimlikleri
        var businessId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        // When — DELETE /api/businesses/{id}/documents/{docId} çağrılır
        var response = await _fixture.Client.DeleteAsync(
            $"/api/businesses/{businessId}/documents/{documentId}");

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BusinessInstructorDocumentEndpoints — /api/businesses/{businessId}/instructor-document
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Usta_ogretici_belge_yukleme_form_disi_govde_ile_500_donmez()
    {
        // Given — var olmayan işletme + multipart olmayan (JSON) gövde
        var businessId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/instructor-document çağrılır (form-data değil)
        var response = await _fixture.Client.PostAsync(
            $"/api/businesses/{businessId}/instructor-document/", EmptyJson());

        // Then — "Multipart form-data bekleniyor" bad request (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_usta_ogretici_belgesi_gecersiz_kilinmaya_calisilinca_500_donmez()
    {
        // Given — var olmayan işletme ve belge kimlikleri + boş gövde
        var businessId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/instructor-document/{docId}/invalidate çağrılır
        var response = await _fixture.Client.PostAsync(
            $"/api/businesses/{businessId}/instructor-document/{documentId}/invalidate", EmptyJson());

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_usta_ogretici_belgesi_silinmeye_calisilinca_500_donmez()
    {
        // Given — var olmayan işletme ve belge kimlikleri + boş gövde
        var businessId = Guid.NewGuid();
        var documentId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/instructor-document/{docId}/delete çağrılır
        var response = await _fixture.Client.PostAsync(
            $"/api/businesses/{businessId}/instructor-document/{documentId}/delete", EmptyJson());

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Usta_ogretici_belge_talebi_bos_govdeyle_500_donmez()
    {
        // Given — var olmayan işletme + boş/geçersiz gövde
        var businessId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/instructor-document/request çağrılır
        var response = await _fixture.Client.PostAsync(
            $"/api/businesses/{businessId}/instructor-document/request", EmptyJson());

        // Then — validation/not-found reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Var_olmayan_isletme_suspend_edilmeye_calisilinca_500_donmez()
    {
        // Given — var olmayan işletme + boş gövde
        var businessId = Guid.NewGuid();

        // When — POST /api/businesses/{id}/suspend çağrılır
        var response = await _fixture.Client.PostAsync($"/api/businesses/{businessId}/suspend", EmptyJson());

        // Then — not-found/validation reddi beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Auth — temsilci endpoint'lerde token'sız istek 401 döner
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Token_olmadan_isletme_listesi_istenince_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — GET /api/businesses çağrılır
        var response = await _fixture.Anonymous.GetAsync("/api/businesses/?page=1&pageSize=20");

        // Then — kimlik doğrulama gerekli → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_olmadan_isletme_detayi_istenince_401_doner()
    {
        // Given — token'sız (anonim) client + rastgele işletme kimliği
        var businessId = Guid.NewGuid();

        // When — GET /api/businesses/{id} çağrılır
        var response = await _fixture.Anonymous.GetAsync($"/api/businesses/{businessId}");

        // Then — kimlik doğrulama gerekli → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
