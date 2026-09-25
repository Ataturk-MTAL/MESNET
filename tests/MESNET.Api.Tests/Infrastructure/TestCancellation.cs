using Xunit;

namespace MESNET.Api.Tests.Infrastructure;

/// <summary>
/// Testin iptal belirteci (xunit.v3, xUnit1051). Test zaman aşımına uğradığında ya da koşu
/// durdurulduğunda bekleyen HTTP/veritabanı çağrıları da iptal olur. Kısa ad, çağrıların
/// okunur uzunlukta kalması için: <c>GetAsync(url, Ct)</c>.
/// </summary>
internal static class TestCancellation
{
    public static CancellationToken Ct => TestContext.Current.CancellationToken;
}
