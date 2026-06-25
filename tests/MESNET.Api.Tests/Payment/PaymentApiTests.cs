using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Payment;

/// <summary>
/// Payment modülü HTTP endpoint'lerinin black-box davranış testleri (BDD-style).
/// Mutasyon yapan happy-path senaryoları YAZILMAZ — sadece auth, not-found,
/// validation-reddi ve liste-okuma kapsanır (paylaşılan dev DB kirletilmez).
///
/// Kapsanan route'lar (group prefix: /api/payments, RequireAuthorization):
///   GET    /api/payments/{id:guid}
///   GET    /api/payments/
///   POST   /api/payments/{id:guid}/upload-receipt/business
///   POST   /api/payments/{id:guid}/upload-receipt/student
///   POST   /api/payments/{id:guid}/confirm
///   POST   /api/payments/{id:guid}/approve/teacher
///   POST   /api/payments/{id:guid}/approve/deputy
///   POST   /api/payments/{id:guid}/reject
///   PUT    /api/payments/config/minimum-wage
/// </summary>
[Collection("api")]
public sealed class PaymentApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJson() => new("{}", Encoding.UTF8, "application/json");

    // ── Auth ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Token_olmadan_odeme_listesi_istenince_401_doner()
    {
        // Given — token'sız (anonim) istemci
        // When — auth gerektiren liste endpoint'ine istek atılır
        var response = await _fixture.Anonymous.GetAsync("/api/payments/");

        // Then — kimlik doğrulama zorunlu → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Token_olmadan_odeme_detayi_istenince_401_doner()
    {
        // Given — token'sız (anonim) istemci ve rastgele bir ödeme kimliği
        var id = Guid.NewGuid();

        // When — auth gerektiren detay endpoint'ine istek atılır
        var response = await _fixture.Anonymous.GetAsync($"/api/payments/{id}");

        // Then — kimlik doğrulama zorunlu → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── GET detay /{id:guid} — EN ÖNEMLİ: null-return 500 bug'ını yakalar ───────────

    [Fact]
    public async Task Olmayan_odeme_detayi_istenince_sunucu_hatasi_donmemeli()
    {
        // Given — hiç var olmayan rastgele bir ödeme kimliği
        var id = Guid.NewGuid();

        // When — o ödemenin özeti istenir
        var response = await _fixture.Client.GetAsync($"/api/payments/{id}");

        // Then — kayıt yok = 404/422 beklenir, ASLA 500 (null-return sunucu hatası DEĞİL)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ── GET liste / ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Odeme_listesi_authed_istekte_basariyla_doner()
    {
        // Given — geçerli token'lı admin istemci (tüm filtreler opsiyonel)
        // When — sayfalı ödeme listesi istenir
        var response = await _fixture.Client.GetAsync("/api/payments/");

        // Then — liste okuması başarılı olmalı (en azından sunucu hatası olmamalı)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Odeme_listesi_filtre_ve_sayfalama_parametreleriyle_basariyla_doner()
    {
        // Given — opsiyonel filtreler + sayfalama/sıralama/arama parametreleri
        var academicPeriodId = Guid.NewGuid();
        var url = $"/api/payments/?academicPeriodId={academicPeriodId}" +
                  "&phase=Pending&page=1&pageSize=10&sortBy=month&descending=true&search=test";

        // When — filtreli sayfalı liste istenir
        var response = await _fixture.Client.GetAsync(url);

        // Then — filtreler geçerli → başarılı, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── POST upload-receipt (multipart, validation-reddi) ───────────────────────────

    [Fact]
    public async Task Isletme_dekontu_form_olmadan_yuklenince_sunucu_hatasi_donmemeli()
    {
        // Given — multipart form-data yerine boş JSON gövde (geçersiz içerik tipi)
        var id = Guid.NewGuid();

        // When — işletme dekontu yükleme endpoint'ine geçersiz gövdeyle istek atılır
        var response = await _fixture.Client.PostAsync(
            $"/api/payments/{id}/upload-receipt/business", EmptyJson());

        // Then — "Multipart form-data bekleniyor" → 400 BadRequest, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ogrenci_dekontu_form_olmadan_yuklenince_sunucu_hatasi_donmemeli()
    {
        // Given — multipart form-data yerine boş JSON gövde (geçersiz içerik tipi)
        var id = Guid.NewGuid();

        // When — öğrenci dekontu yükleme endpoint'ine geçersiz gövdeyle istek atılır
        var response = await _fixture.Client.PostAsync(
            $"/api/payments/{id}/upload-receipt/student", EmptyJson());

        // Then — "Multipart form-data bekleniyor" → 400 BadRequest, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── POST confirm / approve / reject (body'li command, validation-reddi) ─────────

    [Fact]
    public async Task Maas_onayi_bos_govdeyle_istenince_sunucu_hatasi_donmemeli()
    {
        // Given — boş/geçersiz JSON gövde ve rastgele bir ödeme kimliği
        var id = Guid.NewGuid();

        // When — öğrenci maaş alma onayı endpoint'ine boş gövdeyle istek atılır
        var response = await _fixture.Client.PostAsync(
            $"/api/payments/{id}/confirm", EmptyJson());

        // Then — validation/domain reddi (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Ogretmen_onayi_bos_govdeyle_istenince_sunucu_hatasi_donmemeli()
    {
        // Given — boş/geçersiz JSON gövde ve rastgele bir ödeme kimliği
        var id = Guid.NewGuid();

        // When — koordinatör öğretmen dekont onayı endpoint'ine boş gövdeyle istek atılır
        var response = await _fixture.Client.PostAsync(
            $"/api/payments/{id}/approve/teacher", EmptyJson());

        // Then — validation/domain reddi (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Mudur_yardimcisi_onayi_bos_govdeyle_istenince_sunucu_hatasi_donmemeli()
    {
        // Given — boş/geçersiz JSON gövde ve rastgele bir ödeme kimliği
        var id = Guid.NewGuid();

        // When — müdür yardımcısı dekont onayı endpoint'ine boş gövdeyle istek atılır
        var response = await _fixture.Client.PostAsync(
            $"/api/payments/{id}/approve/deputy", EmptyJson());

        // Then — validation/domain reddi (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Dekont_reddi_bos_govdeyle_istenince_sunucu_hatasi_donmemeli()
    {
        // Given — boş/geçersiz JSON gövde ve rastgele bir ödeme kimliği
        var id = Guid.NewGuid();

        // When — dekont reddi endpoint'ine boş gövdeyle istek atılır
        var response = await _fixture.Client.PostAsync(
            $"/api/payments/{id}/reject", EmptyJson());

        // Then — validation/domain reddi (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ── PUT config/minimum-wage (body'li command, validation-reddi) ─────────────────

    [Fact]
    public async Task Asgari_ucret_guncellemesi_bos_govdeyle_istenince_sunucu_hatasi_donmemeli()
    {
        // Given — boş/geçersiz JSON gövde (geçerli asgari ücret parametreleri yok)
        // When — asgari ücret güncelleme endpoint'ine boş gövdeyle istek atılır
        var response = await _fixture.Client.PutAsync(
            "/api/payments/config/minimum-wage", EmptyJson());

        // Then — validation/domain reddi (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Asgari_ucret_guncellemesi_token_olmadan_istenince_401_doner()
    {
        // Given — token'sız (anonim) istemci
        // When — auth gerektiren parametre güncelleme endpoint'ine istek atılır
        var response = await _fixture.Anonymous.PutAsync(
            "/api/payments/config/minimum-wage", EmptyJson());

        // Then — kimlik doğrulama zorunlu → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
