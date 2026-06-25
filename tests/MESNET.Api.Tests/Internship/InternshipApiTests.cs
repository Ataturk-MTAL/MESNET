using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Internship;

/// <summary>
/// MESNET.Internship.Api/InternshipEndpoints.cs içindeki tüm HTTP endpoint'leri için
/// BDD-style black-box davranış testleri. Group prefix: /api/internships
///
/// Kapsam: liste okuma, var olmayan kaydın 500 değil 404/422 döndürmesi (null-return bug yakalayıcı),
/// kimlik doğrulama (401) ve gövdeli mutasyon endpoint'lerinin geçersiz gövdeyi reddetmesi (4xx).
///
/// KASITLI OLARAK YOK: happy-path fesih/onay akışları — gerçek veri mutasyonu paylaşılan dev DB'yi kirletir.
/// Onay (approve) endpoint'leri gövdesizdir; rastgele kimlikle çağrılır ve yalnızca sunucu hatası
/// vermediği doğrulanır (kayıt yok = iş kuralı reddi, 500 değil).
/// </summary>
[Collection("api")]
public sealed class InternshipApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyBody() =>
        new("{}", Encoding.UTF8, "application/json");

    // ──────────────────────────────────────────────────────────────
    // GET /api/internships/{internshipId:guid} — detay
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_staj_detayi_istenince_500_donmemeli()
    {
        // Given — sistemde karşılığı olmayan rastgele bir staj kimliği
        var internshipId = Guid.NewGuid();

        // When — staj detayı istenir
        var response = await _fixture.Client.GetAsync($"/api/internships/{internshipId}");

        // Then — kayıt yok = geçerli durum (404/422), ASLA sunucu hatası (500) değil
        // (null dönen handler'ın 500'e dönüşmesini yakalayan en kritik test)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────
    // GET /api/internships/ — sayfalı liste
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Staj_listesi_istenince_basariyla_doner()
    {
        // Given — kimliği doğrulanmış admin kullanıcı (geniş izinli)

        // When — staj listesi istenir
        var response = await _fixture.Client.GetAsync("/api/internships/");

        // Then — liste okuma her zaman başarılı olmalı (sunucu hatası DEĞİL)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Staj_listesi_filtre_ve_sayfalama_parametreleriyle_basariyla_doner()
    {
        // Given — opsiyonel filtreler (studentId/businessId/academicPeriodId/phase) + sayfalama parametreleri
        var studentId = Guid.NewGuid();
        var academicPeriodId = Guid.NewGuid();

        // When — filtrelenmiş, sayfalı liste istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/internships/?studentId={studentId}&academicPeriodId={academicPeriodId}" +
            "&phase=Active&minAbsenceDays=1&page=1&pageSize=10&sortBy=createdAt&descending=true&search=test");

        // Then — eşleşme bulunmasa bile boş sayfalı sonuç döner, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──────────────────────────────────────────────────────────────
    // Auth — token'sız erişim reddi
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Token_olmadan_staj_listesi_istenince_401_doner()
    {
        // Given — kimlik doğrulaması yapılmamış (token'sız) istemci

        // When — auth gerektiren liste endpoint'ine istek atılır
        var response = await _fixture.Anonymous.GetAsync("/api/internships/");

        // Then — yetkisiz erişim reddedilir (401 Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_olmadan_staj_detayi_istenince_401_doner()
    {
        // Given — kimlik doğrulaması yapılmamış istemci ve rastgele staj kimliği
        var internshipId = Guid.NewGuid();

        // When — auth gerektiren detay endpoint'ine istek atılır
        var response = await _fixture.Anonymous.GetAsync($"/api/internships/{internshipId}");

        // Then — yetkisiz erişim reddedilir (401 Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/internships/{id}/terminate — gövdeli fesih talebi
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Fesih_talebi_gecersiz_govdeyle_gonderilince_500_donmemeli()
    {
        // Given — var olmayan staj + boş/geçersiz JSON gövde
        var internshipId = Guid.NewGuid();

        // When — fesih talebi gönderilir
        var response = await _fixture.Client.PostAsync(
            $"/api/internships/{internshipId}/terminate", EmptyBody());

        // Then — validation/iş kuralı reddi (4xx) beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Token_olmadan_fesih_talebi_gonderilince_401_doner()
    {
        // Given — token'sız istemci
        var internshipId = Guid.NewGuid();

        // When — fesih endpoint'ine yetkisiz istek atılır
        var response = await _fixture.Anonymous.PostAsync(
            $"/api/internships/{internshipId}/terminate", EmptyBody());

        // Then — 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/internships/{id}/approve/parent — veli onayı (gövdesiz)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_staj_icin_veli_onayi_500_donmemeli()
    {
        // Given — var olmayan rastgele staj kimliği
        var internshipId = Guid.NewGuid();

        // When — veli onayı verilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/internships/{internshipId}/approve/parent", EmptyBody());

        // Then — kayıt yok = iş kuralı reddi (4xx/422), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/internships/{id}/approve/teacher — koordinatör öğretmen onayı
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_staj_icin_ogretmen_onayi_500_donmemeli()
    {
        // Given — var olmayan rastgele staj kimliği
        var internshipId = Guid.NewGuid();

        // When — koordinatör öğretmen onayı verilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/internships/{internshipId}/approve/teacher", EmptyBody());

        // Then — kayıt yok = iş kuralı reddi, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/internships/{id}/approve/deputy — müdür yardımcısı onayı
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_staj_icin_mudur_yardimcisi_onayi_500_donmemeli()
    {
        // Given — var olmayan rastgele staj kimliği
        var internshipId = Guid.NewGuid();

        // When — müdür yardımcısı onayı verilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/internships/{internshipId}/approve/deputy", EmptyBody());

        // Then — kayıt yok = iş kuralı reddi, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/internships/{id}/approve/director — müdür onayı
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_staj_icin_mudur_onayi_500_donmemeli()
    {
        // Given — var olmayan rastgele staj kimliği
        var internshipId = Guid.NewGuid();

        // When — müdür onayı verilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/internships/{internshipId}/approve/director", EmptyBody());

        // Then — kayıt yok = iş kuralı reddi, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/internships/{id}/approve/business — işletme yetkilisi onayı
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Var_olmayan_staj_icin_isletme_yetkilisi_onayi_500_donmemeli()
    {
        // Given — var olmayan rastgele staj kimliği
        var internshipId = Guid.NewGuid();

        // When — işletme yetkilisi onayı verilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/internships/{internshipId}/approve/business", EmptyBody());

        // Then — kayıt yok = iş kuralı reddi, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────
    // POST /api/internships/{id}/approve/override — onay zinciri override (gövdeli)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Override_gecersiz_govdeyle_gonderilince_500_donmemeli()
    {
        // Given — var olmayan staj + boş/geçersiz JSON gövde
        var internshipId = Guid.NewGuid();

        // When — onay zinciri override edilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/internships/{internshipId}/approve/override", EmptyBody());

        // Then — validation/iş kuralı reddi (4xx) beklenir, sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Token_olmadan_override_gonderilince_401_doner()
    {
        // Given — token'sız istemci
        var internshipId = Guid.NewGuid();

        // When — override endpoint'ine yetkisiz istek atılır
        var response = await _fixture.Anonymous.PostAsync(
            $"/api/internships/{internshipId}/approve/override", EmptyBody());

        // Then — 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
