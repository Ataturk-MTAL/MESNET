using System.Reflection;
using MESNET.Payment.Application.Consumers;
using Shouldly;
using Wolverine.Configuration;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// <c>AbsenceTallyConsumer</c>'ın yerel kuyruğu <b>sıralı</b> olmalı (#262).
///
/// <para><b>Ölçülen tehlike:</b> <c>MultipleHandlerBehavior.Separated</c> her handler tipine ayrı
/// bir "sticky" yerel kuyruk verir, ama o kuyruk varsayılan olarak <b>paraleldir</b>. Wolverine
/// 6.15.0 üzerinde ölçüldü: yapılandırılmamış sticky kuyrukta
/// <c>MaxDegreeOfParallelism = 12</c> (işlemci sayısı), <c>Sequential()</c> uygulananda
/// <c>= 1</c>.</para>
///
/// <para><b>Kırılma:</b> bu sınıfın tüm handler metotları aynı kuyruğa düşer. Kaydın durumunu
/// değiştiren olay (<c>AttendanceApproved</c>), kaydı <b>kuran</b> olayı
/// (<c>AttendanceMarked</c>) geçerse yerel satır henüz yoktur; güncelleme düşer ve kayıt kalıcı
/// olarak <c>Pending</c> kalır. Onaylanmış bir devamsızlık bir daha hiçbir olay üretmediği için
/// kendiliğinden düzelmez. <c>UseDurableLocalQueues()</c> dayanıklılık verir, <b>sıra
/// vermez</b>.</para>
///
/// <para><b>Sınır — dürüstçe:</b> bu test <b>yapıyı</b> kilitler (arayüz uygulanıyor mu,
/// <c>Sequential()</c> çağrılıyor mu). Wolverine'in bunu gerçekten uyguladığı ayrıca ölçüldü ama
/// birim testte host ayağa kaldırılmıyor.</para>
/// </summary>
public sealed class AbsenceTallyConsumerQueueOrderingTests
{
    [Fact]
    public void Kuyruk_yapilandirmasi_taniml()
    {
        typeof(IConfigureLocalQueue).IsAssignableFrom(typeof(AbsenceTallyConsumer))
            .ShouldBeTrue(
                "Sticky yerel kuyruk varsayılan olarak paraleldir; sıralı yapılmazsa aynı kayda "
                + "ait olaylar birbirini geçer ve güncelleme sessizce düşer.");
    }

    /// <summary>
    /// Sınıf <b>statik olamaz</b> — statik sınıf arayüz uygulayamaz. Metotlar statik kalır;
    /// Wolverine statik handler metotlarını örnek oluşturmadan çağırır (ölçüldü).
    /// </summary>
    [Fact]
    public void Handler_metotlari_statik_kalir()
    {
        var handlerMetotlari = typeof(AbsenceTallyConsumer)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.Name is "Consume" or "ConsumeAsync" or "Handle" or "HandleAsync")
            .ToList();

        handlerMetotlari.ShouldNotBeEmpty("Handler metotları kaybolmuş — keşif kırılır.");
    }

    [Fact]
    public void Sequential_cagriliyor()
    {
        var kaynak = File.ReadAllText(SourcePath());

        kaynak.Contains("configuration.Sequential()", StringComparison.Ordinal).ShouldBeTrue(
            "Configure yalnız arayüzü doldurmak için var olamaz; kuyruğu gerçekten sıralı yapmalı.");
    }

    private static string SourcePath() => Path.Combine(RepoRoot(), "src/Modules/Payment/MESNET.Payment.Application/Consumers/AbsenceTallyConsumer.cs");

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx).");
    }
}
