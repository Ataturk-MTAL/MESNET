using System.Net;
using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Coordination;

/// <summary>
/// GET /api/coordination/teachers/{id}/schedule/current davranış testleri.
/// "Kayıtlı program yok" geçerli bir boş durumdur (404), sunucu hatası (500) değil.
/// </summary>
[Collection("api")]
public sealed class CurrentScheduleApiTests(ApiTestFixture fixture)
{
    private readonly ApiTestFixture _fixture = fixture;

    [Fact]
    public async Task Programi_olmayan_ogretmen_doneminde_gecerli_program_istenince_404_doner_500_degil()
    {
        // Given — hiç ders programı kaydı olmayan bir öğretmen ve dönem (rastgele kimlikler)
        var teacherId = Guid.NewGuid();
        var academicPeriodId = Guid.NewGuid();

        // When — o öğretmen/dönem için güncel ders programı istenir
        var response = await _fixture.Client.GetAsync(
            $"/api/coordination/teachers/{teacherId}/schedule/current" +
            $"?academicPeriodId={academicPeriodId}&semester=Fall");

        // Then — program yok = geçerli boş durum → 404 (Not Found), sunucu hatası DEĞİL
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
