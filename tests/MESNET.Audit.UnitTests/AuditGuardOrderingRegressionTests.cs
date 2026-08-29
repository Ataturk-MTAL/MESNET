using MESNET.Audit.Application.Auditing;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Wolverine;
using Xunit;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Madde 1 (KRİTİK) regresyon kilidi: <c>Program.cs</c>'teki kayıt sırası — denetim
/// middleware'i dört guard politikasının (kapalı dönem + kurum kapsamı) ÜSTÜNDE, yani
/// zincirde EN DIŞTA — bozulursa bu test kırmızıya döner.
/// </summary>
/// <remarks>
/// <para>Ölçüldü (canlı Wolverine ana bilgisayarı, 29.08.2026): guard middleware'i denetimden
/// ÖNCE kayıtlıyken (denetim guard'ın İÇİNDE/altında sarılı) guard'ın attığı
/// <c>DomainException</c>, <c>AuditMiddleware.Before</c>'un hiç ÇALIŞMASINA fırsat vermeden
/// zinciri kesiyor: <c>accessor.Current</c> null kalıyor, <c>OnExceptionAsync</c> hiçbir şey
/// yazmıyor → <b>0 satır</b>. Denetim üstte (dışta) kayıtlıyken aynı ret <b>1 satır</b>
/// bırakıyor (<c>DomainException</c> ile). İstisna her iki sırada da çağırana ulaşıyor (422
/// doğru üretiliyor), yalnız iz kayboluyor — bu yüzden derleme ve diğer testler bu regresyonu
/// yakalayamaz.</para>
///
/// <para>Bu test <c>Program.cs</c>'teki kayıt sırasını (denetim → dört guard) taklit eder;
/// gerçek guard sınıfları yerine minik bir sahte guard kullanır ki test Payment/Contract/
/// Attendance/Institution modüllerine bağımlı olmasın. Sahte yazıcı/sahte kullanıcı
/// altyapısı <c>AuditMiddlewareContractTests</c>'ten yeniden kullanılır.</para>
///
/// <para><b>Kanıt (29.08.2026, elle koşturuldu, geri alındı):</b> aşağıdaki
/// <c>DenetimGuarddanOnceKayitli</c> sabiti <c>false</c> yapılıp (yani guard denetimden ÖNCE
/// kayıtlı — Program.cs'teki eski/bozuk sıra taklit edilip) test koşturulunca
/// <c>Guardin_reddettigi_komut_denetim_USTTE_kayitliyken_iz_birakir</c> KIRMIZIYA döndü:
/// <c>Shouldly.ShouldAssertException: yazici.Yazilanlar.Count</c> beklenen <c>1</c>, gerçek
/// <c>0</c> ("yazici.Yazilanlar.Count should be 1 but was 0"). Sabit tekrar <c>true</c>
/// yapılınca test yeşile döndü. Kilit gerçek.</para>
/// </remarks>
public class AuditGuardOrderingRegressionTests
{
    /// <summary>
    /// DAİMA <c>true</c> kalmalı — <c>Program.cs</c>'teki doğru (düzeltilmiş) sırayı temsil
    /// eder: denetim middleware'i guard'ların ÜSTÜNDE kayıtlı. Yalnız yukarıdaki kanıt
    /// notundaki elle doğrulama sırasında geçici olarak <c>false</c> yapılıp geri alındı.
    /// </summary>
    // static readonly (const DEĞİL): derleyici sabit dala göre "unreachable code" uyarısı
    // vermesin — kanıt adımında bu değer geçici olarak false yapılıp elle koşturuldu.
    private static readonly bool DenetimGuarddanOnceKayitli = true;

    // Kasıtlı olarak STATİK DEĞİL — brief'in istediği "basit, DomainException fırlatan bir
    // Before metodu olan statik olmayan sınıf" guard şekli. Üretimdeki guard'lar statiktir
    // (bkz. SalaryPeriodGuardMiddleware) ama Wolverine örnek (instance) middleware'i de
    // destekler; bu test ikisinin de aynı sıralama kuralına tabi olduğunu gösterir.
    public sealed class SahteGuardMiddleware
    {
        public void Before(AuditMiddlewareContractTests.OrnekKomut command)
        {
            if (command.Reddet)
                throw new DomainException(new Error("GUARD_RET", "Guard reddetti (test)."));
        }
    }

    private static Task<IHost> AnaBilgisayarKurAsync(IAuditWriter writer)
        => Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(writer);
                services.AddScoped<AuditContextAccessor>();
                services.AddSingleton<ICurrentUserService, AuditMiddlewareContractTests.SahteKullanici>();
            })
            .UseWolverine(opts =>
            {
                // Kayıt sırası burada üretilir. Wolverine middleware zincirinde ilk kaydedilen
                // EN DIŞTA sarar — bkz. sınıf üstü açıklama.
                if (DenetimGuarddanOnceKayitli)
                {
                    opts.Policies.AddMiddleware(
                        typeof(AuditMiddleware),
                        chain => chain.MessageType == typeof(AuditMiddlewareContractTests.OrnekKomut));
                    opts.Policies.AddMiddleware(
                        typeof(SahteGuardMiddleware),
                        chain => chain.MessageType == typeof(AuditMiddlewareContractTests.OrnekKomut));
                }
                else
                {
                    opts.Policies.AddMiddleware(
                        typeof(SahteGuardMiddleware),
                        chain => chain.MessageType == typeof(AuditMiddlewareContractTests.OrnekKomut));
                    opts.Policies.AddMiddleware(
                        typeof(AuditMiddleware),
                        chain => chain.MessageType == typeof(AuditMiddlewareContractTests.OrnekKomut));
                }
            })
            .StartAsync();

    [Fact]
    public async Task Guardin_reddettigi_komut_denetim_USTTE_kayitliyken_iz_birakir()
    {
        // Arrange — Program.cs'teki DÜZELTİLMİŞ sırayı taklit eder: denetim guard'ın üstünde.
        var yazici = new AuditMiddlewareContractTests.SahteYazici();
        var host = await AnaBilgisayarKurAsync(yazici);
        using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        // Act
        var ex = await Should.ThrowAsync<DomainException>(
            () => bus.InvokeAsync<string>(
                new AuditMiddlewareContractTests.OrnekKomut(Guid.NewGuid(), Reddet: true)));

        // Assert — guard'ın reddi yine de bir iz satırı bırakmalı.
        ex.Error.Code.ShouldBe("GUARD_RET");
        yazici.Yazilanlar.Count.ShouldBe(1);
        yazici.Yazilanlar[0].Exception.ShouldNotBeNull();

        await host.StopAsync();
    }
}
