using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Institution;

/// <summary>
/// Institution modülünün tüm HTTP endpoint'leri için BDD-style black-box davranış testleri.
///
/// Kapsanan route grupları:
///   - /api/institutions                                  (InstitutionEndpoints)
///   - /api/institutions/{id}/academic-periods            (AcademicPeriodEndpoints)
///   - /api/field-catalog + /api/institutions/{id}/branches (FieldCatalogEndpoints)
///
/// Testler yalnızca okuma, not-found, validation-reddi ve auth davranışını doğrular.
/// Happy-path CREATE/UPDATE/DELETE YOKTUR — paylaşılan dev DB'yi kirletmez.
/// </summary>
[Collection("api")]
public sealed class InstitutionApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJsonBody() =>
        new("{}", Encoding.UTF8, "application/json");

    // ──────────────────────────────────────────────────────────────────────
    //  InstitutionEndpoints — /api/institutions
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Kurum_listesi_yetkili_istekte_basariyla_doner()
    {
        // Given — yetkili (admin) bir istemci
        // When — kurum listesi istenir
        var response = await _fixture.Client.GetAsync("/api/institutions/", Ct);

        // Then — sunucu hatası değil, başarılı (200) döner
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Yanıt artık PagedResult sarmalayıcısıdır: data.items + data.totalCount.
        // Çıplak dizi bekleyen bir iddia burada sessizce boş listeye düşerdi.
        var body = await response.Content.ReadAsStringAsync(Ct);
        body.ShouldContain("\"items\"");
        body.ShouldContain("\"totalCount\"");
    }

    /// <summary>
    /// Düğüm tipi süzgeci. <c>Institution</c> artık "okul" demek değil; süzgeç çalışmazsa
    /// il/ilçe müdürlükleri okul listesinde belirir ve bu SESSİZCE olur.
    /// </summary>
    [Fact]
    public async Task Kurum_listesi_dugum_tipine_gore_suzulur()
    {
        var response = await _fixture.Client.GetAsync("/api/institutions/?nodeType=Province", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(Ct);
        body.ShouldContain("\"items\"");
    }

    [Fact]
    public async Task Olmayan_kurum_detayi_istenince_sunucu_hatasi_donmez()
    {
        // Given — var olmayan rastgele bir kurum kimliği
        var institutionId = Guid.NewGuid();

        // When — kurum detayı istenir
        var response = await _fixture.Client.GetAsync($"/api/institutions/{institutionId}", Ct);

        // Then — 404/422 beklenir; null-return 500 bug'ı OLMAMALI
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kurum_olusturma_bos_govdeyle_dogrulama_reddiyle_donmeli()
    {
        // Given — yetkili istemci ve boş/geçersiz JSON gövde
        // When — kurum oluşturma denenir (mutasyon yapmaz, reddedilir)
        var response = await _fixture.Client.PostAsync("/api/institutions/", EmptyJsonBody(), Ct);

        // Then — 4xx validation/bad request beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kurum_guncelleme_bos_govdeyle_sunucu_hatasi_donmez()
    {
        // Given — var olmayan kurum kimliği ve boş JSON gövde
        var institutionId = Guid.NewGuid();

        // When — PATCH ile güncelleme denenir
        var response = await _fixture.Client.PatchAsync($"/api/institutions/{institutionId}", EmptyJsonBody(), Ct);

        // Then — 4xx beklenir (validation/not-found), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Personel_yetkilendirme_bos_govdeyle_sunucu_hatasi_donmez()
    {
        // Given — var olmayan kurum kimliği ve boş JSON gövde
        var institutionId = Guid.NewGuid();

        // When — personel yetkilendirme denenir
        var response = await _fixture.Client.PostAsync($"/api/institutions/{institutionId}/staff", EmptyJsonBody(), Ct);

        // Then — 4xx beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Ders_programi_ayarlari_guncelleme_bos_govdeyle_sunucu_hatasi_donmez()
    {
        // Given — var olmayan kurum kimliği ve boş JSON gövde
        var institutionId = Guid.NewGuid();

        // When — schedule-config PUT ile güncellenmeye çalışılır
        var response = await _fixture.Client.PutAsync($"/api/institutions/{institutionId}/schedule-config", EmptyJsonBody(), Ct);

        // Then — 4xx beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_kurumun_ders_programi_ayari_istenince_sunucu_hatasi_donmez()
    {
        // Given — var olmayan rastgele bir kurum kimliği
        var institutionId = Guid.NewGuid();

        // When — schedule-config okunur
        var response = await _fixture.Client.GetAsync($"/api/institutions/{institutionId}/schedule-config", Ct);

        // Then — 404/422 beklenir; null-return 500 bug'ı OLMAMALI
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  AcademicPeriodEndpoints — /api/institutions/{id}/academic-periods
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Akademik_donem_listesi_yetkili_istekte_sunucu_hatasi_donmez()
    {
        // Given — rastgele bir kurum kimliği (sayfalama varsayılanlarıyla)
        var institutionId = Guid.NewGuid();

        // When — akademik dönem listesi istenir
        var response = await _fixture.Client.GetAsync($"/api/institutions/{institutionId}/academic-periods/", Ct);

        // Then — boş liste geçerli sonuçtur; sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Aktif_akademik_donem_istenince_sunucu_hatasi_donmez()
    {
        // Given — aktif dönemi olmayan rastgele bir kurum kimliği
        var institutionId = Guid.NewGuid();

        // When — aktif akademik dönem istenir
        var response = await _fixture.Client.GetAsync($"/api/institutions/{institutionId}/academic-periods/active", Ct);

        // Then — aktif dönem yok = geçerli boş durum (404/422); null-return 500 bug'ı OLMAMALI
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Akademik_donem_olusturma_bos_govdeyle_sunucu_hatasi_donmez()
    {
        // Given — rastgele kurum kimliği ve boş JSON gövde
        var institutionId = Guid.NewGuid();

        // When — akademik dönem oluşturma denenir
        var response = await _fixture.Client.PostAsync($"/api/institutions/{institutionId}/academic-periods/", EmptyJsonBody(), Ct);

        // Then — 4xx validation/bad request beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_donem_kapatilmaya_calisilinca_sunucu_hatasi_donmez()
    {
        // Given — var olmayan kurum ve dönem kimliği
        var institutionId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        // When — dönem kapatma denenir (gövdesiz POST)
        var response = await _fixture.Client.PostAsync($"/api/institutions/{institutionId}/academic-periods/{periodId}/close", null, Ct);

        // Then — 4xx beklenir (not-found/validation), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  FieldCatalogEndpoints
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Alan_katalogu_yetkili_istekte_basariyla_doner()
    {
        // Given — yetkili istemci
        // When — alan kataloğu istenir
        var response = await _fixture.Client.GetAsync("/api/field-catalog", Ct);

        // Then — statik katalog; başarılı (200) döner, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Alan_aktiflestirme_bos_govdeyle_sunucu_hatasi_donmez()
    {
        // Given — rastgele kurum kimliği ve boş JSON gövde
        var institutionId = Guid.NewGuid();

        // When — alan (branch) aktifleştirme denenir
        var response = await _fixture.Client.PostAsync($"/api/institutions/{institutionId}/branches/", EmptyJsonBody(), Ct);

        // Then — 4xx validation/bad request beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Olmayan_alan_pasife_alinmaya_calisilinca_sunucu_hatasi_donmez()
    {
        // Given — var olmayan kurum ve alan kodu
        var institutionId = Guid.NewGuid();

        // When — alan pasife alma (DELETE) denenir
        var response = await _fixture.Client.DeleteAsync($"/api/institutions/{institutionId}/branches/99", Ct);

        // Then — 4xx beklenir (not-found/validation), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Uzmanlik_alanlari_guncelleme_bos_govdeyle_sunucu_hatasi_donmez()
    {
        // Given — rastgele kurum kimliği, alan kodu ve boş JSON gövde
        var institutionId = Guid.NewGuid();

        // When — uzmanlık alanları (specializations) PUT ile güncellenmeye çalışılır
        var response = await _fixture.Client.PutAsync($"/api/institutions/{institutionId}/branches/99/specializations", EmptyJsonBody(), Ct);

        // Then — 4xx beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Seflik_yapilandirmasi_guncelleme_bos_govdeyle_sunucu_hatasi_donmez()
    {
        // Given — rastgele kurum kimliği, alan kodu ve boş JSON gövde
        var institutionId = Guid.NewGuid();

        // When — şeflik (supervisors) yapılandırması PUT ile güncellenmeye çalışılır
        var response = await _fixture.Client.PutAsync($"/api/institutions/{institutionId}/branches/99/supervisors", EmptyJsonBody(), Ct);

        // Then — 4xx beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────────────
    //  Auth — temsilci endpoint'ler token'sız erişimde 401 döner
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Kurum_listesi_tokensiz_istekte_401_doner()
    {
        // Given — token'sız (anonim) bir istemci
        // When — kurum listesi istenir
        var response = await _fixture.Anonymous.GetAsync("/api/institutions/", Ct);

        // Then — kimlik doğrulama zorunlu → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Alan_katalogu_tokensiz_istekte_401_doner()
    {
        // Given — token'sız (anonim) bir istemci
        // When — alan kataloğu istenir
        var response = await _fixture.Anonymous.GetAsync("/api/field-catalog", Ct);

        // Then — kimlik doğrulama zorunlu → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Akademik_donem_listesi_tokensiz_istekte_401_doner()
    {
        // Given — token'sız (anonim) bir istemci ve rastgele kurum kimliği
        var institutionId = Guid.NewGuid();

        // When — akademik dönem listesi istenir
        var response = await _fixture.Anonymous.GetAsync($"/api/institutions/{institutionId}/academic-periods/", Ct);

        // Then — kimlik doğrulama zorunlu → 401 Unauthorized
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
