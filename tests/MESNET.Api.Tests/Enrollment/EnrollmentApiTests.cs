using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Enrollment;

/// <summary>
/// Enrollment modülünün TÜM HTTP endpoint'leri için black-box (BDD-style) davranış testleri.
/// Kapsam: Teacher (/api/teachers), Student (/api/students),
/// Placement (/api/placements), Application (/api/internship-applications).
///
/// İlke: mutasyon yapan happy-path CREATE/UPDATE/DELETE testleri YAZILMAZ
/// (paylaşılan dev DB'yi kirletir). Sadece şu desenler uygulanır:
///   - liste okuma (GET) → 200/OK (en azından 500 DEĞİL)
///   - detay GET /{id} rastgele kimlikle → 404/422 beklenir, sunucu hatası (500) DEĞİL
///   - auth → token'sız istek 401 (Unauthorized)
///   - POST/PUT/PATCH boş/geçersiz gövde ile → validation reddi (4xx), 500 DEĞİL (mutasyon YOK)
/// </summary>
[Collection("api")]
public sealed class EnrollmentApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJson() =>
        new("{}", Encoding.UTF8, "application/json");

    // ───────────────────────── Teacher endpoints ─────────────────────────
    // group: /api/teachers (RequireAuthorization)
    //   POST /              RegisterTeacher
    //   GET  /{teacherId}   detay
    //   GET  /              liste (paged)

    [Fact]
    public async Task Ogretmen_listesi_yetkili_istekte_basariyla_doner()
    {
        // Given — yetkili admin client
        // When — öğretmen listesi istenir
        var response = await _fixture.Client.GetAsync("/api/teachers/");

        // Then — liste okuma sunucu hatası vermez ve OK döner
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Var_olmayan_ogretmen_detayi_istenince_404_doner_500_degil()
    {
        // Given — kayıtlı olmayan rastgele bir öğretmen kimliği
        var teacherId = Guid.NewGuid();

        // When — o öğretmenin detayı istenir
        var response = await _fixture.Client.GetAsync($"/api/teachers/{teacherId}");

        // Then — null dönüş = 404 (Not Found), sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ogretmen_listesi_anonim_istekte_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — yetki gerektiren öğretmen listesi istenir
        var response = await _fixture.Anonymous.GetAsync("/api/teachers/");

        // Then — kimlik doğrulaması zorunlu → 401 (Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ogretmen_kaydi_gecersiz_govdeyle_reddedilir_500_degil()
    {
        // Given — yetkili client + boş/geçersiz JSON gövde
        // When — eksik veriyle öğretmen kaydı denenir
        var response = await _fixture.Client.PostAsync("/api/teachers/", EmptyJson());

        // Then — validation reddi (4xx) beklenir; mutasyon olmaz, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ───────────────────────── Student endpoints ─────────────────────────
    // group: /api/students (RequireAuthorization)
    //   POST  /                          RegisterStudent
    //   PATCH /{studentId}               UpdateStudentProfile
    //   POST  /{studentId}/deregister    DeregisterStudentRequest
    //   POST  /sync-counts               SyncStudentCounts
    //   GET   /{studentId}               detay
    //   GET   /                          liste (paged)

    [Fact]
    public async Task Ogrenci_listesi_yetkili_istekte_basariyla_doner()
    {
        // Given — yetkili admin client
        // When — öğrenci listesi istenir
        var response = await _fixture.Client.GetAsync("/api/students/");

        // Then — liste okuma sunucu hatası vermez ve OK döner
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Var_olmayan_ogrenci_detayi_istenince_404_doner_500_degil()
    {
        // Given — kayıtlı olmayan rastgele bir öğrenci kimliği
        var studentId = Guid.NewGuid();

        // When — o öğrencinin detayı istenir
        var response = await _fixture.Client.GetAsync($"/api/students/{studentId}");

        // Then — null dönüş = 404 (Not Found), sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Ogrenci_listesi_anonim_istekte_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — yetki gerektiren öğrenci listesi istenir
        var response = await _fixture.Anonymous.GetAsync("/api/students/");

        // Then — kimlik doğrulaması zorunlu → 401 (Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ogrenci_kaydi_gecersiz_govdeyle_reddedilir_500_degil()
    {
        // Given — yetkili client + boş/geçersiz JSON gövde
        // When — eksik veriyle öğrenci kaydı denenir
        var response = await _fixture.Client.PostAsync("/api/students/", EmptyJson());

        // Then — validation reddi (4xx) beklenir; mutasyon olmaz, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    [Fact]
    public async Task Ogrenci_profil_guncelleme_gecersiz_govdeyle_reddedilir_500_degil()
    {
        // Given — rastgele öğrenci kimliği + boş/geçersiz JSON gövde
        var studentId = Guid.NewGuid();

        // When — eksik veriyle PATCH ile profil güncelleme denenir
        var response = await _fixture.Client.PatchAsync($"/api/students/{studentId}", EmptyJson());

        // Then — validation/not-found reddi (4xx); mutasyon olmaz, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    [Fact]
    public async Task Var_olmayan_ogrenci_kaydi_silinmek_istenince_500_donmez()
    {
        // Given — kayıtlı olmayan öğrenci kimliği + zorunlu Reason eksik gövde
        var studentId = Guid.NewGuid();

        // When — deregister denenir (boş gövde → Reason eksik)
        var response = await _fixture.Client.PostAsync(
            $"/api/students/{studentId}/deregister", EmptyJson());

        // Then — validation/not-found/domain reddi (4xx); sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    [Fact]
    public async Task Ogrenci_kayit_silme_anonim_istekte_401_doner()
    {
        // Given — token'sız (anonim) client + rastgele öğrenci kimliği
        var studentId = Guid.NewGuid();

        // When — yetki gerektiren deregister endpoint'i çağrılır
        var response = await _fixture.Anonymous.PostAsync(
            $"/api/students/{studentId}/deregister", EmptyJson());

        // Then — kimlik doğrulaması zorunlu → 401 (Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ogrenci_sayilarini_senkronize_etme_gecersiz_govdeyle_500_donmez()
    {
        // Given — yetkili client + boş/geçersiz JSON gövde
        // When — sync-counts boş gövdeyle çağrılır
        var response = await _fixture.Client.PostAsync("/api/students/sync-counts", EmptyJson());

        // Then — validation/işleme sonucu sunucu hatası vermemeli (4xx ya da OK)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ───────────────────────── Placement endpoints ─────────────────────────
    // group: /api/placements (RequireAuthorization)
    //   POST /                          PlaceStudent
    //   POST /{placementId}/mark-failed MarkAsFailedToComplete
    //   GET  /{placementId}             detay
    //   GET  /                          liste (paged)

    [Fact]
    public async Task Yerlestirme_listesi_yetkili_istekte_basariyla_doner()
    {
        // Given — yetkili admin client
        // When — yerleştirme listesi istenir
        var response = await _fixture.Client.GetAsync("/api/placements/");

        // Then — liste okuma sunucu hatası vermez ve OK döner
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Var_olmayan_yerlestirme_detayi_istenince_404_doner_500_degil()
    {
        // Given — kayıtlı olmayan rastgele bir yerleştirme kimliği
        var placementId = Guid.NewGuid();

        // When — o yerleştirmenin detayı istenir
        var response = await _fixture.Client.GetAsync($"/api/placements/{placementId}");

        // Then — null dönüş = 404 (Not Found), sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Yerlestirme_listesi_anonim_istekte_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — yetki gerektiren yerleştirme listesi istenir
        var response = await _fixture.Anonymous.GetAsync("/api/placements/");

        // Then — kimlik doğrulaması zorunlu → 401 (Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Yerlestirme_olusturma_gecersiz_govdeyle_reddedilir_500_degil()
    {
        // Given — yetkili client + boş/geçersiz JSON gövde
        // When — eksik veriyle öğrenci yerleştirme denenir
        var response = await _fixture.Client.PostAsync("/api/placements/", EmptyJson());

        // Then — validation reddi (4xx) beklenir; mutasyon olmaz, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    [Fact]
    public async Task Var_olmayan_yerlestirme_basarisiz_isaretlenince_500_donmez()
    {
        // Given — kayıtlı olmayan rastgele yerleştirme kimliği
        var placementId = Guid.NewGuid();

        // When — yerleştirme 'tamamlayamadı' olarak işaretlenmeye çalışılır (boş gövde)
        var response = await _fixture.Client.PostAsync(
            $"/api/placements/{placementId}/mark-failed", EmptyJson());

        // Then — not-found/domain reddi (4xx); sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    // ──────────────── Yerleştirme projeksiyon backfill'i (#185) ────────────────
    // POST /api/placements/resync-projections
    //
    // Bu uç bir DAĞITIM ADIMIDIR: yeni bir read-model eklendiğinde mevcut kayıtlar ancak
    // buradan dolar (bkz. Docs → Altyapı → Dağıtım Ön Koşulları). Adım atlanırsa hata çıkmaz,
    // liste boş gelir. Belgede yazılı yolun kayması da aynı biçimde sessiz olurdu — #185 zaten
    // YANLIŞ bir yol tahmin etmişti (/api/enrollment/students/resync-projections). Aşağıdaki
    // testler doğru yolu ve operatörün okuması gereken yanıt alanlarını kilitler.

    [Fact]
    public async Task Yerlestirme_projeksiyon_resync_ucu_anonim_istekte_401_doner()
    {
        // Given — token'sız client
        // When — backfill ucu çağrılır
        var response = await _fixture.Anonymous.PostAsync(
            "/api/placements/resync-projections", EmptyJson());

        // Then — dağıtım adımı da yetki ister
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Uç, belgede yazılı yolda durmalı ve <c>placementCount</c>/<c>skipped</c> döndürmeli.
    /// Operatöre "skipped sıfırdan büyükse bak" deniyor; alan kaybolursa o yönerge sessizce
    /// anlamsızlaşır.
    /// </summary>
    [Fact]
    public async Task Yerlestirme_projeksiyon_resync_sayaclariyla_doner()
    {
        // Given — yetkili client
        // When — backfill çağrılır (idempotent: tüketiciler upsert yapar)
        var response = await _fixture.Client.PostAsync(
            "/api/placements/resync-projections", EmptyJson());

        // Then — uç bu yolda vardır ve sunucu hatası vermez
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Then — yanıt iki sayacı da taşır
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("placementCount");
        body.ShouldContain("skipped");
    }

    // ──────────────────── Internship Application endpoints ────────────────────
    // group: /api/internship-applications (RequireAuthorization)
    //   POST /          ApplyForInternship
    //   POST /request   RequestStudent

    [Fact]
    public async Task Staj_basvurusu_gecersiz_govdeyle_reddedilir_500_degil()
    {
        // Given — yetkili client + boş/geçersiz JSON gövde
        // When — eksik veriyle staj başvurusu denenir
        var response = await _fixture.Client.PostAsync("/api/internship-applications/", EmptyJson());

        // Then — validation reddi (4xx) beklenir; mutasyon olmaz, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }

    [Fact]
    public async Task Staj_basvurusu_anonim_istekte_401_doner()
    {
        // Given — token'sız (anonim) client
        // When — yetki gerektiren staj başvurusu endpoint'i çağrılır
        var response = await _fixture.Anonymous.PostAsync(
            "/api/internship-applications/", EmptyJson());

        // Then — kimlik doğrulaması zorunlu → 401 (Unauthorized)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Isletme_ogrenci_talebi_gecersiz_govdeyle_reddedilir_500_degil()
    {
        // Given — yetkili client + boş/geçersiz JSON gövde
        // When — eksik veriyle işletme öğrenci talebi denenir
        var response = await _fixture.Client.PostAsync(
            "/api/internship-applications/request", EmptyJson());

        // Then — validation reddi (4xx) beklenir; mutasyon olmaz, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        ((int)response.StatusCode).ShouldBeGreaterThanOrEqualTo(400);
        ((int)response.StatusCode).ShouldBeLessThan(500);
    }
}
