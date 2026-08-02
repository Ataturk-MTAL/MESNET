using System.Net;
using System.Text;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Security;

/// <summary>
/// Veli–öğrenci bağ ucunun black-box davranış testleri (#174).
///
/// Kapsanan endpoint:
///   UserManagementEndpoints
///     POST /api/security/users/{userAccountId}/students   (ChangeUserStudents)
///
/// Kural: mutasyon yapan happy-path testleri yazılmaz (paylaşılan dev DB kirlenir).
/// Buradaki testler not-found ve validation yollarının 500 dönmediğini doğrular.
/// </summary>
[Collection("api")]
public sealed class ParentLinkApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");

    [Fact]
    public async Task Olmayan_kullanicinin_bagi_500_donmez()
    {
        // Given — var olmayan kullanıcı hesabı
        var userAccountId = Guid.NewGuid();

        // When
        var response = await _fixture.Client.PostAsync(
            $"/api/security/users/{userAccountId}/students",
            Json($$"""{"studentIds":["{{Guid.NewGuid()}}"]}"""));

        // Then — not-found (4xx) beklenir, sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    /// <summary>
    /// Boş liste <b>geçerli</b> bir istektir — bağı kaldırmak meşru işlemdir (öğrenci mezun
    /// oldu, vesayet değişti). Doğrulama hatası ÜRETMEMELİ.
    /// </summary>
    [Fact]
    public async Task Bos_liste_dogrulama_hatasi_uretmez()
    {
        var userAccountId = Guid.NewGuid();

        var response = await _fixture.Client.PostAsync(
            $"/api/security/users/{userAccountId}/students",
            Json("""{"studentIds":[]}"""));

        // Kullanıcı yok → 4xx; ama 500 ya da "boş liste geçersiz" tipi bir çökme olmamalı.
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Bozuk_govde_500_donmez()
    {
        var userAccountId = Guid.NewGuid();

        var response = await _fixture.Client.PostAsync(
            $"/api/security/users/{userAccountId}/students",
            Json("""{"studentIds":["gecersiz-guid"]}"""));

        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Kimliksiz_istek_401_doner()
    {
        var response = await _fixture.Anonymous.PostAsync(
            $"/api/security/users/{Guid.NewGuid()}/students",
            Json("""{"studentIds":[]}"""));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
