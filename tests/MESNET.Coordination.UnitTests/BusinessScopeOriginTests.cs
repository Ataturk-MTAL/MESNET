using MESNET.Coordination.Application.Helpers;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// İşletme <b>provenance</b>'ının koordinasyon <b>kapsamına</b> çevrimi (ADR-0003 adım 4).
///
/// <para><b>Neden uyarı test ediliyor:</b> boş provenance sessiz kalırsa görünüm
/// <c>Guid.Empty</c> kapsamıyla yazılır ve işletme koordinasyon ekranlarından kaybolur —
/// hata yok, log yok, boş liste. Canlıda ölçüldü: göç edilmemiş belge onaylandığında görünüm
/// gerçekten <c>00000000-0000-0000-0000-000000000000</c> kapsamıyla açıldı. Tek sinyal bu
/// uyarıdır; düşerse kimse fark etmez.</para>
/// </summary>
public sealed class BusinessScopeOriginTests
{
    private static readonly Guid Institution = Guid.Parse("efd57b88-2f47-471c-9f51-476f80fabfca");
    private static readonly Guid BusinessId = Guid.Parse("1df52898-e202-4601-bab6-df678e8fa8ac");

    [Fact]
    public void Dolu_provenance_oldugu_gibi_doner_ve_uyarmaz()
    {
        var logger = new CapturingLogger();

        BusinessScopeOrigin.Resolve(Institution, BusinessId, logger).ShouldBe(Institution);

        logger.Warnings.ShouldBeEmpty("Sağlıklı durumda uyarı çıkmamalı; yoksa uyarı gürültüye döner.");
    }

    /// <summary>
    /// Boş provenance yalnızca <c>institutionId</c> → <c>registeredByInstitutionId</c> JSON
    /// göçü atlanmış belgelerden gelir: kayıt uçları kurum kapsamı olmadan işletme
    /// oluşturmayı zaten reddeder.
    /// </summary>
    [Fact]
    public void Bos_provenance_uyari_uretir()
    {
        var logger = new CapturingLogger();

        BusinessScopeOrigin.Resolve(Guid.Empty, BusinessId, logger).ShouldBe(Guid.Empty);

        logger.Warnings.Count.ShouldBe(1);
        logger.Warnings[0].ShouldContain(BusinessId.ToString(),
            customMessage: "Uyarı hangi işletmeyi kastettiğini söylemeli, yoksa aranamaz.");
    }

    /// <summary>
    /// <b>Değer düzeltilmez.</b> Uydurulmuş bir kurum kimliği, kaybolan işletmeden daha
    /// kötüdür: veri yanlış kiracıya yazılır ve bu sessizdir.
    /// </summary>
    [Fact]
    public void Bos_deger_uydurulmaz()
    {
        BusinessScopeOrigin.Resolve(Guid.Empty, BusinessId, new CapturingLogger())
            .ShouldBe(Guid.Empty);
    }

    private sealed class CapturingLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Warning)
                Warnings.Add(formatter(state, exception));
        }
    }
}
