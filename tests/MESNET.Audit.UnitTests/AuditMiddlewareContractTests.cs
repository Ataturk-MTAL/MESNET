using MESNET.Audit.Application.Auditing;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Wolverine;
using Xunit;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Denetim middleware'inin CANLI Wolverine ana bilgisayarındaki sözleşmesi.
/// </summary>
/// <remarks>
/// <para><b>Bu testin varlık nedeni ölçülmüş iki davranıştır:</b></para>
/// <list type="number">
/// <item><b>Reddedilen komut iz bırakmalıdır.</b> Wolverine'in <c>AutoApplyTransactions()</c>
/// politikası <c>DomainException</c>'da işlemi geri alır; denetim satırı aynı oturumda
/// yazılsaydı ret kaydı da geri alınırdı. Ayrı oturum kararını bu test kilitler.</item>
/// <item><b><c>OnException</c> istisnayı YUTAR.</b> Rethrow silinirse
/// <c>DomainException</c> HTTP katmanına hiç ulaşmaz: 422 yerine 200 döner ve reddedilen
/// her komut başarılı görünür. Derleme geçer, diğer birim testleri geçer, log temiz kalır.
/// Bu testin ikinci iddiası tam olarak o sessiz felakete karşıdır.</item>
/// </list>
///
/// <para>Sahte yazıcı kullanılır — Marten/PostgreSQL gerekmez. Ölçülen şey yazıcının
/// ÇAĞRILDIĞI ve istisnanın ÇAĞIRANA ULAŞTIĞIDIR.</para>
/// </remarks>
public class AuditMiddlewareContractTests
{
    // NOT: brief'teki taslakta `private` idi ama OrnekKomutHandler.Handle PUBLIC ve Wolverine
    // handler keşfi PUBLIC tip ister (CS0051: erişilebilirlik çelişkisi, ölçüldü) — bu yüzden
    // komut tipi de public.
    public sealed record OrnekKomut(Guid StudentId, bool Reddet);

    public static class OrnekKomutHandler
    {
        public static string Handle(OrnekKomut command)
        {
            if (command.Reddet)
                throw new DomainException(new Error("KURAL_IHLALI", "İş kuralı izin vermedi."));

            return "tamam";
        }
    }

    private sealed class SahteYazici : IAuditWriter
    {
        public List<(string CommandType, Exception? Exception)> Yazilanlar { get; } = [];

        public Task WriteAsync(AuditContext context, Exception? exception, CancellationToken ct = default)
        {
            Yazilanlar.Add((context.CommandType.Name, exception));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Her çağrıda fırlayan sahte yazıcı. Hata sözleşmesinin <see cref="IAuditWriter"/>
    /// UYGULAMASINA bağımlı OLMADIĞINI kanıtlamak için — üretim <c>AuditWriter</c>'ı zaten
    /// kendi içinde her şeyi yakalıyor, ama middleware bunu VARSAYMAMALI.
    /// </summary>
    private sealed class FirlatanYazici : IAuditWriter
    {
        public Task WriteAsync(AuditContext context, Exception? exception, CancellationToken ct = default)
            => throw new InvalidOperationException("Denetim yazıcısı bozuldu (test).");
    }

    private sealed class SahteKullanici : ICurrentUserService
    {
        private static readonly UserContext Kullanici = new(
            UserId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FullName: "Ayşe Öğretmen",
            InstitutionId: Guid.Parse("22222222-2222-2222-2222-222222222222"));

        public UserContext? GetCurrentUser() => Kullanici;
        public Guid GetUserId() => Kullanici.UserId;
        public string GetFullName() => Kullanici.FullName;
        public bool HasPermission(string permission) => false;
        public bool IsInRole(string role) => false;
        public IReadOnlyList<string> GetBranchCodes() => [];
        public IReadOnlyList<Guid> GetLinkedStudentIds() => [];
        public string? GetInstitutionPath() => "/il/ilce/okul/";
    }

    private static async Task<(IHost Host, SahteYazici Yazici)> AnaBilgisayarKurAsync()
    {
        var yazici = new SahteYazici();
        var host = await AnaBilgisayarKurAsync(yazici);
        return (host, yazici);
    }

    /// <summary>Verilen yazıcıyla canlı ana bilgisayarı kurar — Bulgu 1 testleri için.</summary>
    private static Task<IHost> AnaBilgisayarKurAsync(IAuditWriter writer)
        => Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton<IAuditWriter>(writer);
                services.AddScoped<AuditContextAccessor>();
                services.AddSingleton<ICurrentUserService, SahteKullanici>();
            })
            .UseWolverine(opts =>
            {
                opts.Policies.AddMiddleware(
                    typeof(AuditMiddleware),
                    chain => chain.MessageType == typeof(OrnekKomut));
            })
            .StartAsync();

    [Fact]
    public async Task Basarili_komut_bir_iz_satiri_birakir()
    {
        // Arrange
        var (host, yazici) = await AnaBilgisayarKurAsync();
        using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        // Act
        var sonuc = await bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: false));

        // Assert
        sonuc.ShouldBe("tamam");
        yazici.Yazilanlar.Count.ShouldBe(1);
        yazici.Yazilanlar[0].CommandType.ShouldBe(nameof(OrnekKomut));
        yazici.Yazilanlar[0].Exception.ShouldBeNull();

        await host.StopAsync();
    }

    [Fact]
    public async Task Reddedilen_komut_da_iz_satiri_birakir()
    {
        // AYRI OTURUM kararının kilidi: aynı oturuma dönülürse bu satır geri alınırdı.
        var (host, yazici) = await AnaBilgisayarKurAsync();
        using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        await Should.ThrowAsync<DomainException>(
            () => bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: true)));

        yazici.Yazilanlar.Count.ShouldBe(1);
        yazici.Yazilanlar[0].Exception.ShouldBeOfType<DomainException>();

        await host.StopAsync();
    }

    [Fact]
    public async Task Reddedilen_komutun_istisnasi_CAGIRANA_ULASIR()
    {
        // OnException rethrow'unun kilidi. Rethrow silinirse Wolverine istisnayı yutar,
        // DomainException HTTP katmanına ulaşmaz ve 422 yerine 200 döner — reddedilen her
        // komut başarılı görünür. Ölçüldü.
        var (host, _) = await AnaBilgisayarKurAsync();
        using var __ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        var ex = await Should.ThrowAsync<DomainException>(
            () => bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: true)));

        ex.Error.Code.ShouldBe("KURAL_IHLALI");

        await host.StopAsync();
    }

    [Fact]
    public async Task Reddedilen_komut_TEK_satir_birakir()
    {
        // Finally hem başarıda hem başarısızlıkta çalışır; koşul kalkarsa istisna yolunda
        // İKİ satır doğar (biri yanlışlıkla "başarılı").
        var (host, yazici) = await AnaBilgisayarKurAsync();
        using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        await Should.ThrowAsync<DomainException>(
            () => bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: true)));

        yazici.Yazilanlar.Count.ShouldBe(1);

        await host.StopAsync();
    }

    [Fact]
    public async Task Yazici_firlatsa_bile_basarili_komut_bozulmaz()
    {
        // Bulgu 1'in kilidi (başarı yolu): FinallyAsync'teki writer.WriteAsync try/catch İLE
        // SARILI olmalı. Sarılı olmasaydı FirlatanYazici'nin istisnası komutun kendi
        // sonucunun YERİNE geçer ve "tamam" yerine InvalidOperationException fırlardı —
        // hata sözleşmesi yazıcı uygulamasına bağımlı olurdu.
        var host = await AnaBilgisayarKurAsync(new FirlatanYazici());
        using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        var sonuc = await bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: false));

        sonuc.ShouldBe("tamam");

        await host.StopAsync();
    }

    [Fact]
    public async Task Yazici_firlatsa_bile_reddedilen_komutun_DomainException_i_CAGIRANA_ULASIR()
    {
        // Bulgu 1'in kilidi (ret yolu): OnExceptionAsync'teki writer.WriteAsync try/catch İLE
        // SARILI olmalı ve rethrow bunun DIŞINDA kalmalı. Sarılı olmasaydı
        // FirlatanYazici'nin istisnası ExceptionDispatchInfo satırına hiç ULAŞILMADAN
        // çağırana giderdi — orijinal DomainException'ın yerini denetim istisnası alırdı
        // (422 yerine 500).
        var host = await AnaBilgisayarKurAsync(new FirlatanYazici());
        using var _ = host;
        var bus = host.Services.GetRequiredService<IMessageBus>();

        var ex = await Should.ThrowAsync<DomainException>(
            () => bus.InvokeAsync<string>(new OrnekKomut(Guid.NewGuid(), Reddet: true)));

        ex.Error.Code.ShouldBe("KURAL_IHLALI");

        await host.StopAsync();
    }
}
