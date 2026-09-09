using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Alan/dönem parametresi için kapsam kilidi (#311).
///
/// <para><b>Neden kilit gerekiyor:</b> alan bazlı koordinasyon satırı
/// <c>(businessId, branchCode, academicPeriodId)</c> üçlüsüyle bulunur. Eksik parametreyi boş
/// değere çevirmek — <c>branchCode ?? string.Empty</c>, <c>academicPeriodId ?? Guid.Empty</c> —
/// deterministik kimliği <b>temel satıra</b> düşürür ve
/// <c>CoordinationViewLookup.LoadBranchRowAsync</c> alan satırı taramasına hiç geçmez
/// (<c>row is not null</c> erken döner).</para>
///
/// <para><b>Ölçüldü:</b> ataması ve 2 geçmiş kaydı olan bir işletmede
/// <c>GET /assignments/{id}/history</c> alan/dönem verilmeden <c>200</c> ve <b>boş liste</b>
/// döndü — hata değil, yanlış bilgi. Aynı varsayılan iki silme ucunda da vardı; orada veri
/// bozulmuyordu ama "işletme atanmamış" diyen yanıltıcı bir 422 üretiyordu.</para>
///
/// <para><b>Doğru davranış eksik parametrede AÇILMAMAKTIR:</b> parametre zorunlu olunca ASP.NET
/// <c>400</c> döner. Yarı kapsamlı bir sorgu, hiç çalışmayan sorgudan kötüdür — sessizce yanlış
/// satırı okur ya da yazar.</para>
/// </summary>
public sealed class CoordinationScopeParameterDriftTests
{
    private static readonly Regex EmptyScopeFallback = new(
        @"\?\?\s*(Guid\.Empty|string\.Empty)", RegexOptions.Compiled);

    private static string EndpointsSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src")))
            dir = dir.Parent;

        dir.ShouldNotBeNull("Depo kökü bulunamadı — test çalışma dizininden yukarı çıkamadı.");

        var path = Path.Combine(
            dir.FullName, "src", "Modules", "Coordination",
            "MESNET.Coordination.Api", "CoordinationEndpoints.cs");

        File.Exists(path).ShouldBeTrue($"Uç dosyası bulunamadı: {path}");
        return File.ReadAllText(path);
    }

    [Fact]
    public void Kapsam_parametresi_bos_degere_cevrilmez()
    {
        // Arrange
        var source = EndpointsSource();

        // Act
        var hits = EmptyScopeFallback.Matches(source).Select(m => m.Value).ToList();

        // Assert
        hits.ShouldBeEmpty(
            "Eksik alan/dönem parametresini boş değere çevirmek deterministik kimliği temel " +
            "satıra düşürür ve sorgu sessizce yanlış satırı okur (#311). Parametreyi ZORUNLU " +
            "yapın (`string branchCode`, `Guid academicPeriodId`) — eksikse 400 dönsün.");
    }

    [Theory]
    [InlineData("GetAssignmentHistory")]
    [InlineData("DeleteAssignment")]
    [InlineData("DeleteAssignmentSlot")]
    public void Satir_bazli_uclar_alan_ve_donemi_zorunlu_ister(string handlerName)
    {
        // Arrange
        var source = EndpointsSource();
        var start = source.IndexOf($"IResult> {handlerName}(", StringComparison.Ordinal);
        start.ShouldBeGreaterThan(-1, $"Handler bulunamadı: {handlerName}");

        // Act — imza bloğu: açılış parantezinden gövdenin başına kadar
        var bodyStart = source.IndexOf('{', start);
        var signature = source[start..bodyStart];

        // Assert
        signature.ShouldContain("string branchCode",
            customMessage: $"{handlerName}: branchCode zorunlu olmalı (`string?` değil).");
        signature.ShouldContain("Guid academicPeriodId",
            customMessage: $"{handlerName}: academicPeriodId zorunlu olmalı (`Guid?` değil).");
    }
}
