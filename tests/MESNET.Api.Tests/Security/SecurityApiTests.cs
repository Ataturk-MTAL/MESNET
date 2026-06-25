using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Security;

/// <summary>
/// Security modülünün tüm HTTP endpoint'leri için black-box davranış testleri.
/// Kapsam: davet (invitation), rol (role) ve kullanıcı yönetimi (user management) endpoint'leri.
/// İlke: gerçek veri OLUŞTURMA/GÜNCELLEME/SİLME (paylaşılan dev DB kirlenir) yapılmaz.
/// Sadece liste-okuma, not-found, validation-reddi ve auth (401) davranışları doğrulanır.
/// "Kayıt yok" geçerli bir boş durumdur (404/422) — sunucu hatası (500) DEĞİL.
/// </summary>
[Collection("api")]
public sealed class SecurityApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent EmptyJsonBody() =>
        new("{}", Encoding.UTF8, "application/json");

    // ──────────────────────────────────────────────────────────────────────
    // InvitationEndpoints — /api/security/invitations
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Davet_listesi_yetkili_istekte_basariyla_doner()
    {
        // Given — yetkili (admin) bir istemci
        // When — davet listesi istenir
        var response = await _fixture.Client.GetAsync("/api/security/invitations/");

        // Then — liste okuması başarılı olmalı, sunucu hatası olmamalı
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Davet_listesi_token_olmadan_401_doner()
    {
        // Given — token'sız (anonim) istemci
        // When — davet listesi istenir
        var response = await _fixture.Anonymous.GetAsync("/api/security/invitations/");

        // Then — yetkilendirme gerektiren endpoint 401 döndürmeli
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Davet_olusturma_bos_govdeyle_sunucu_hatasi_vermez()
    {
        // Given — yetkili istemci ve boş/geçersiz JSON gövde
        // When — davet oluşturma istenir
        var response = await _fixture.Client.PostAsync("/api/security/invitations/", EmptyJsonBody());

        // Then — validation reddi beklenir (4xx), sunucu hatası DEĞİL (mutasyon yapılmaz)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Davet_onaylama_olmayan_kimlikle_sunucu_hatasi_vermez()
    {
        // Given — rastgele (var olmayan) davet kimliği ve boş gövde
        var invitationId = Guid.NewGuid();

        // When — davet onaylanmak istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/security/invitations/{invitationId}/approve", EmptyJsonBody());

        // Then — 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Davet_reddetme_olmayan_kimlikle_sunucu_hatasi_vermez()
    {
        // Given — rastgele (var olmayan) davet kimliği ve boş gövde
        var invitationId = Guid.NewGuid();

        // When — davet reddedilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/security/invitations/{invitationId}/reject", EmptyJsonBody());

        // Then — 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Davet_tamamlama_anonim_erisilebilir_ve_sunucu_hatasi_vermez()
    {
        // Given — bu endpoint AllowAnonymous'tur; rastgele davet kimliği ve boş gövde
        var invitationId = Guid.NewGuid();

        // When — anonim istemci ile davet tamamlanmak istenir
        var response = await _fixture.Anonymous.PostAsync(
            $"/api/security/invitations/{invitationId}/complete", EmptyJsonBody());

        // Then — AllowAnonymous → 401 OLMAMALI; var olmayan davet için 4xx, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Davet_yeniden_gonderme_olmayan_kimlikle_sunucu_hatasi_vermez()
    {
        // Given — rastgele (var olmayan) davet kimliği ve boş gövde
        var invitationId = Guid.NewGuid();

        // When — davet e-postası yeniden gönderilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/security/invitations/{invitationId}/resend", EmptyJsonBody());

        // Then — 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    // ──────────────────────────────────────────────────────────────────────
    // RoleEndpoints — /api/security
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Rol_listesi_yetkili_istekte_basariyla_doner()
    {
        // Given — yetkili istemci
        // When — tüm roller istenir
        var response = await _fixture.Client.GetAsync("/api/security/roles");

        // Then — statik rol listesi başarıyla dönmeli
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Rol_listesi_token_olmadan_401_doner()
    {
        // Given — token'sız istemci
        // When — roller istenir
        var response = await _fixture.Anonymous.GetAsync("/api/security/roles");

        // Then — yetkilendirme gerektiren endpoint 401 döndürmeli
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Olmayan_rolun_izinleri_istenince_404_doner_500_degil()
    {
        // Given — var olmayan bir rol adı
        var roleName = "OlmayanRol_" + Guid.NewGuid().ToString("N");

        // When — o rolün izinleri istenir
        var response = await _fixture.Client.GetAsync($"/api/security/roles/{roleName}/permissions");

        // Then — bilinmeyen rol = geçerli not-found durumu → 404, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Tum_izinler_yetkili_istekte_basariyla_doner()
    {
        // Given — yetkili istemci
        // When — tüm izin kataloğu istenir
        var response = await _fixture.Client.GetAsync("/api/security/permissions");

        // Then — statik izin listesi başarıyla dönmeli
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ──────────────────────────────────────────────────────────────────────
    // UserManagementEndpoints — /api/security/users
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Kullanici_listesi_yetkili_istekte_basariyla_doner()
    {
        // Given — yetkili istemci
        // When — kullanıcı listesi istenir
        var response = await _fixture.Client.GetAsync("/api/security/users/");

        // Then — sayfalı liste okuması başarılı olmalı
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Kullanici_listesi_token_olmadan_401_doner()
    {
        // Given — token'sız istemci
        // When — kullanıcı listesi istenir
        var response = await _fixture.Anonymous.GetAsync("/api/security/users/");

        // Then — yetkilendirme gerektiren endpoint 401 döndürmeli
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Olmayan_kullanici_detayi_istenince_sunucu_hatasi_vermez()
    {
        // Given — var olmayan bir kullanıcı kimliği
        var userAccountId = Guid.NewGuid();

        // When — o kullanıcının detayı istenir
        var response = await _fixture.Client.GetAsync($"/api/security/users/{userAccountId}");

        // Then — kayıt yok = geçerli durum (404/422), null-return kaynaklı sunucu hatası (500) DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kullanici_olusturma_bos_govdeyle_sunucu_hatasi_vermez()
    {
        // Given — yetkili istemci ve boş/geçersiz JSON gövde
        // When — kullanıcı oluşturma istenir
        var response = await _fixture.Client.PostAsync("/api/security/users/", EmptyJsonBody());

        // Then — validation reddi beklenir (4xx), sunucu hatası DEĞİL (mutasyon yapılmaz)
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kullanici_guncelleme_olmayan_kimlik_ve_bos_govdeyle_sunucu_hatasi_vermez()
    {
        // Given — rastgele kullanıcı kimliği ve boş gövde
        var userAccountId = Guid.NewGuid();

        // When — kullanıcı güncellenmek istenir
        var response = await _fixture.Client.PutAsync(
            $"/api/security/users/{userAccountId}", EmptyJsonBody());

        // Then — validation/not-found beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kullanici_rol_degisikligi_olmayan_kimlik_ve_bos_govdeyle_sunucu_hatasi_vermez()
    {
        // Given — rastgele kullanıcı kimliği ve boş gövde
        var userAccountId = Guid.NewGuid();

        // When — kullanıcı rolleri değiştirilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/security/users/{userAccountId}/roles", EmptyJsonBody());

        // Then — validation/not-found beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kullanici_izin_degisikligi_olmayan_kimlik_ve_bos_govdeyle_sunucu_hatasi_vermez()
    {
        // Given — rastgele kullanıcı kimliği ve boş gövde
        var userAccountId = Guid.NewGuid();

        // When — kullanıcı yetkileri değiştirilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/security/users/{userAccountId}/permissions", EmptyJsonBody());

        // Then — validation/not-found beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kullanici_durum_degistirme_olmayan_kimlik_ve_bos_govdeyle_sunucu_hatasi_vermez()
    {
        // Given — rastgele kullanıcı kimliği ve boş gövde
        var userAccountId = Guid.NewGuid();

        // When — kullanıcı aktif/pasif durumu değiştirilmek istenir
        var response = await _fixture.Client.PostAsync(
            $"/api/security/users/{userAccountId}/toggle-status", EmptyJsonBody());

        // Then — validation/not-found beklenir (4xx), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kullanici_silme_token_olmadan_401_doner()
    {
        // Given — token'sız istemci ve rastgele kullanıcı kimliği
        var userAccountId = Guid.NewGuid();

        // When — kullanıcı silinmek istenir
        var response = await _fixture.Anonymous.DeleteAsync($"/api/security/users/{userAccountId}");

        // Then — yetkilendirme gerektiren endpoint 401 döndürmeli (silme gerçekleşmez)
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Kullanici_silme_olmayan_kimlikle_sunucu_hatasi_vermez()
    {
        // Given — rastgele (var olmayan) kullanıcı kimliği
        var userAccountId = Guid.NewGuid();

        // When — yetkili istemci var olmayan kullanıcıyı silmek ister
        var response = await _fixture.Client.DeleteAsync($"/api/security/users/{userAccountId}");

        // Then — var olmayan kayıt = 404/422 beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }
}
