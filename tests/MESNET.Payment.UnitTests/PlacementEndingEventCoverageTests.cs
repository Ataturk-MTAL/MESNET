using System.Reflection;
using MESNET.Contract.Shared.Events;
using MESNET.Enrollment.Shared.Events;
using MESNET.Institution.Shared.Events;
using MESNET.Payment.Application.Consumers;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Yerleştirmeyi bitiren HER olayın bir tüketicisi olduğunu kilitler (#152).
///
/// <para><b>Neden bu test var:</b> #152 tek bir unutulmuş tüketiciydi ve ikizi
/// (<c>ContractCompleted</c>) aynı anda kayıptı. İkisi de aylarca sessiz durdu çünkü belirti
/// ay sonunda, para tarafında ve dolaylı olarak ortaya çıkıyordu — ayrılmış öğrenci için
/// dekont uyarısı.</para>
///
/// <para>Tekil hatayı düzeltmek yeterli değil: <b>sınıfı</b> kapatmak gerekiyor. Yeni bir
/// yerleştirme-bitiren olay eklenip tüketicisi unutulursa, bu test kırmızı yanar — sessizce
/// yanlış maaş açılmaz.</para>
///
/// <para>Liste bilinçli olarak <b>elle</b> tutulur: "yerleştirmeyi bitirir mi" bir alan
/// kararıdır, tip sisteminden türetilemez. Yeni olay ekleyen kişi listeyi de güncellemek
/// zorunda kalır — asıl istenen budur.</para>
/// </summary>
public sealed class PlacementEndingEventCoverageTests
{
    /// <summary>
    /// Yerleştirmeyi bitiren olaylar. Her biri <c>PlacementView</c>'ı kapatan bir tüketici
    /// bulmak ZORUNDADIR.
    /// </summary>
    public static TheoryData<Type> PlacementEndingEvents =>
    [
        typeof(ContractTerminated),        // fesih — #152'nin bulunduğu yer
        typeof(ContractCompleted),         // başarıyla tamamlama — #152'nin ikizi
        typeof(StudentFailedToComplete),   // staj başarısız
        typeof(StudentDeregistered),       // öğrenci kaydı silindi
    ];

    [Theory]
    [MemberData(nameof(PlacementEndingEvents))]
    public void Yerlestirmeyi_bitiren_her_olayin_tuketicisi_vardir(Type eventType)
    {
        HasConsumerFor(typeof(PlacementViewConsumer), eventType)
            .ShouldBeTrue(
                $"{eventType.Name} yerleştirmeyi bitirir; PlacementViewConsumer onu tüketmek "
                + "ZORUNDADIR (#152). Aksi hâlde ay sonu maaş zamanlayıcısı ayrılmış öğrenciyi "
                + "aktif görür ve yanlış işletmeye dekont yükümlülüğü doğar.");
    }

    /// <summary>
    /// Dönem kapanışı ayrı bir tüketicide (<c>AcademicPeriodClosedConsumer</c>) ele alınır —
    /// tek yerleştirmeyi değil, dönemin tümünü kapattığı için ayrı durması doğrudur.
    /// Burada yalnız o yolun hâlâ var olduğu doğrulanır.
    /// </summary>
    [Fact]
    public void Donem_kapanisi_yerlestirmeleri_kapatmaya_devam_eder()
    {
        HasConsumerFor(typeof(AcademicPeriodClosedConsumer), typeof(AcademicPeriodClosed))
            .ShouldBeTrue("Kapalı dönemde maaş açılmamalıdır (CLAUDE.md — geçmiş dönem salt okunur).");
    }

    private static bool HasConsumerFor(Type consumerType, Type eventType) =>
        consumerType
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.Name is "Consume" or "ConsumeAsync" or "Handle" or "HandleAsync")
            .Select(m => m.GetParameters().FirstOrDefault())
            .Any(p => p?.ParameterType == eventType);
}
