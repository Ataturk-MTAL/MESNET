using System.Reflection;
using MESNET.Coordination.Application.Consumers;
using Shouldly;
using Wolverine.Configuration;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// <c>BranchStudentCountView</c>'ı besleyen tüketicilerin yerel kuyruğu <b>sıralı</b> olmalı (#262).
///
/// <para><b>Ölçülen hata (25.09.2026, temiz seed):</b> Enrollment'ta her alanda 40 öğrenci
/// kayıtlıyken görünüm EET 1, BT 7, MTT 4 gösteriyordu. Tüketici kaydı okuyup sayacı artırıp
/// geri yazıyor; paralel kuyrukta (<c>MaxDegreeOfParallelism = 12</c>) aynı alana ait olaylar
/// aynı eski değeri okuyor ve artışlar birbirini eziyor. <c>RetryOnce</c> yalnız ilk
/// INSERT çakışmasını kapatıyordu, kayıp güncellemeyi değil.</para>
///
/// <para><b>Sonuç sessizdi:</b> alan ders yükü havuzu grup sayısını bu sayaçtan hesaplar; eksik
/// sayım grup sayısını 0'a indirdi ve havuz yalnız şef saatlerinden (16) oluştu — ders saatleri
/// hiç görünmedi. Hata yok, uyarı yok.</para>
///
/// <para><b>Sınır — dürüstçe:</b> test <b>yapıyı</b> kilitler. Üç tüketici ayrı kuyruklardadır;
/// sıralı kuyruk her birinin kendi içindeki yarışı kapatır, üçü arasındakini kapatmaz.
/// Mutlak düzeltme yolu <c>POST /api/students/sync-counts</c>'tur.</para>
/// </summary>
public sealed class BranchStudentCountConsumerQueueOrderingTests
{
    public static TheoryData<Type> Tuketiciler =>
    [
        typeof(StudentRegisteredCountConsumer),
        typeof(StudentDeregisteredCountConsumer),
        typeof(StudentCountsSyncedConsumer),
    ];

    [Theory]
    [MemberData(nameof(Tuketiciler))]
    public void Kuyruk_yapilandirmasi_tanimli(Type tuketici)
    {
        typeof(IConfigureLocalQueue).IsAssignableFrom(tuketici).ShouldBeTrue(
            $"{tuketici.Name}: sticky yerel kuyruk varsayılan olarak paraleldir; okuyup-artırıp-yazan "
            + "tüketici sıralı yapılmazsa artışlar birbirini ezer ve sayaç sessizce eksik kalır.");
    }

    [Theory]
    [MemberData(nameof(Tuketiciler))]
    public void Handler_metotlari_statik_kalir(Type tuketici)
    {
        tuketici.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.Name is "Consume" or "ConsumeAsync")
            .ShouldNotBeEmpty($"{tuketici.Name}: handler metotları kaybolmuş — keşif kırılır.");
    }

    [Theory]
    [MemberData(nameof(Tuketiciler))]
    public void Sequential_cagriliyor(Type tuketici)
    {
        var kaynak = File.ReadAllText(Path.Combine(
            RepoRoot(), "src/Modules/Coordination/MESNET.Coordination.Application/Consumers", $"{tuketici.Name}.cs"));

        kaynak.Contains("configuration.Sequential()", StringComparison.Ordinal).ShouldBeTrue(
            $"{tuketici.Name}: Configure yalnız arayüzü doldurmak için var olamaz; kuyruğu gerçekten sıralı yapmalı.");
    }

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
