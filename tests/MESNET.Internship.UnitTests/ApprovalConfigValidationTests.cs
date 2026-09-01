using MESNET.Internship.Core.Entities;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Eşik doğrulaması. <b>Sıfır ve negatif</b> her açık zinciri tıkanmış yapar — kart anlamını
/// kaybeder. <b>Üst sınır</b> yazım hatasını (14 yerine 1400) kartı sessizce boşaltmadan
/// durdurur.
/// </summary>
public sealed class ApprovalConfigValidationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(14)]
    [InlineData(365)]
    public void Gecerli_esikler_kabul_edilir(int days)
    {
        InternshipApprovalConfig.IsValidThreshold(days).ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(366)]
    [InlineData(1400)]
    public void Gecersiz_esikler_reddedilir(int days)
    {
        InternshipApprovalConfig.IsValidThreshold(days).ShouldBeFalse();
    }

    [Fact]
    public void Varsayilan_esik_14_gundur()
    {
        InternshipApprovalConfig.DefaultStuckApprovalDays.ShouldBe(14);
        new InternshipApprovalConfig().StuckApprovalDays.ShouldBe(14);
    }
}
