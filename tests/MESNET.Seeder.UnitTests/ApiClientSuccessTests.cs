using System.Net;
using System.Text;
using System.Text.Json;
using MESNET.Seeder;
using Shouldly;
using Xunit;

namespace MESNET.Seeder.UnitTests;

/// <summary>
/// Seeder'ın "başarılı mı" ölçütü <b>HTTP durumuna</b> bakmalı, gövdedeki <c>data</c> alanına
/// değil (#204).
///
/// <para><b>Yaşanan hasar:</b> istemci <c>envelope?.Data</c> döndürüyordu, çağıranlar da
/// <c>null</c>'ı başarısızlık sayıyordu. Ama uçların bir kısmı bilerek veri döndürmez —
/// <c>POST /api/institutions/{id}/branches</c> ve <c>PUT /api/payments/config/minimum-wage</c>
/// <c>ResponseBuilder.Success().AddMessage(...)</c> ile yalnız mesaj döner:</para>
///
/// <code>
/// {"code":200,"type":"Success","message":"Asgari ücret güncellendi.","data":null,...}
/// </code>
///
/// <para>Sonucu iki yönlü yalandı: alan aktivasyonu <b>başarılı olduğu hâlde</b> ✗ basıyor ve
/// <c>continue</c> ile bir sonraki satırdaki dal (specialization) yazımını atlıyordu — temiz
/// veritabanında ölçüldü, üç alanın da dalları boş kalıyor. Asgari ücret ise <b>yazıldığı
/// hâlde</b> hiçbir satır basmıyordu.</para>
///
/// <para><b>Neden sessizdi:</b> yanıt 2xx olduğu için <c>FailureCount</c> hiç artmıyor, çıkış
/// kodu sıfır kalıyor, CI yeşil. Sayacın varlık amacı "sessizce yutulan hata kalmasın"dı (#80);
/// burada tersi oluyordu — <b>olmayan</b> hata raporlanıyor, sayaç susuyordu.</para>
/// </summary>
public sealed class ApiClientSuccessTests
{
    /// <summary>
    /// Asıl kural: <c>data</c> taşımayan 2xx <b>başarıdır</b>. Çağıranların tamamı
    /// <c>is null</c> ile başarısızlığı ayırt ettiği için dönüş <c>null</c> OLAMAZ.
    /// </summary>
    [Fact]
    public async Task Data_donmeyen_2xx_yanit_basari_sayilir()
    {
        var handler = new StubHandler("""{"code":200,"type":"Success","message":"Alan aktifleştirildi.","data":null}""");
        var api = CreateClient(handler);

        var sonuc = await api.PostAsync("/api/institutions/x/branches", new { fieldCode = "EET" });

        sonuc.ShouldNotBeNull("Gövdesiz 2xx başarıdır; null dönerse çağıran hata sanar.");
        api.FailureCount.ShouldBe(0);
    }

    [Fact]
    public async Task Data_donmeyen_2xx_put_yaniti_basari_sayilir()
    {
        var handler = new StubHandler("""{"code":200,"type":"Success","message":"Asgari ücret güncellendi.","data":null}""");
        var api = CreateClient(handler);

        var sonuc = await api.PutAsync("/api/payments/config/minimum-wage", new { newMinimumWage = 22104.00m });

        sonuc.ShouldNotBeNull();
        api.FailureCount.ShouldBe(0);
    }

    /// <summary>Veri dönen uçların davranışı değişmemeli — çağıranlar alanları okuyor.</summary>
    [Fact]
    public async Task Data_donen_2xx_yanit_veriyi_dondurur()
    {
        var handler = new StubHandler("""{"code":200,"type":"Success","data":{"id":"42","name":"Deneme"}}""");
        var api = CreateClient(handler);

        var sonuc = await api.PostAsync("/api/businesses", new { name = "Deneme" });

        sonuc.ShouldNotBeNull().GetProperty("id").GetString().ShouldBe("42");
        api.FailureCount.ShouldBe(0);
    }

    /// <summary>
    /// Gerçek hata hâlâ <c>null</c> dönmeli ve sayacı artırmalı — yoksa düzeltme, gürültüyü
    /// susturmak için körlüğü satın almış olur.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Hata_yaniti_null_dondurur_ve_sayaci_artirir(HttpStatusCode durum)
    {
        var handler = new StubHandler("""{"code":403,"type":"Error","message":"Yetkisiz"}""", durum);
        var api = CreateClient(handler);

        var sonuc = await api.PostAsync("/api/institutions/x/branches", new { fieldCode = "EET" });

        sonuc.ShouldBeNull();
        api.FailureCount.ShouldBe(1);
    }

    /// <summary>
    /// <c>GetAsync</c>'in 404 → <c>null</c> davranışı KORUNMALI: orada <c>null</c> "kayıt yok"
    /// demektir ve seeder'ın "zaten var mı" kontrolleri buna dayanır. Bu testin kırılması,
    /// düzeltmenin yanlış yere uygulandığını gösterir.
    /// </summary>
    [Fact]
    public async Task Get_404_null_dondurmeye_devam_eder()
    {
        var handler = new StubHandler("", HttpStatusCode.NotFound);
        var api = CreateClient(handler);

        var sonuc = await api.GetAsync("/api/institutions/yok");

        sonuc.ShouldBeNull("404 'kayıt yok' demektir; başarı sayılamaz.");
        api.FailureCount.ShouldBe(0, "Bulunamama hata değildir.");
    }

    // ── Test çiftleri ────────────────────────────────────────────────────────────────────

    private static MesnetApiClient CreateClient(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5270") },
            new KeycloakTokenService(
                new HttpClient(new StubHandler("""{"access_token":"test-token","expires_in":300}""")),
                new SeederOptions()));

    private sealed class StubHandler(string body, HttpStatusCode status = HttpStatusCode.OK)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }
}
