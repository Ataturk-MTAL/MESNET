using MESNET.Payment.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Payment.UnitTests;

/// <summary>
/// Fesih sonrası yerleştirmenin kapanması regresyonu (#152).
///
/// <para><b>Hata neydi:</b> <c>ContractTerminated</c> olayını yalnız <c>InternshipSaga</c> ve
/// <c>ContractWageConsumer</c> dinliyordu; ikincisi <c>StudentContractWageView</c>'ı kapatıyor
/// ama <c>PlacementView</c>'a hiç dokunmuyordu. Ay sonu maaş zamanlayıcısı çalışma listesini
/// <c>PlacementView.Where(p =&gt; p.IsActive)</c> ile kurduğu için ayrılmış öğrenci hâlâ
/// listedeydi: ayrıldığı işletmeye dekont yükümlülüğü doğuyor, ayın 8'inde gecikme uyarısı
/// gidiyor ve teşvik hesabı o kayıt üzerinden yürüyordu.</para>
///
/// <para><b>Para etkisi:</b> <c>SalaryPeriodId</c> (öğrenci, ay) ikilisinden türetilir. Aynı ay
/// içinde fesih + yeni yerleştirme olan öğrencide bayat eski kayıt önce işlenip maaş dönemini
/// ESKİ işletmeyle açıyor, yeni yerleştirme "zaten var" diye atlanıyordu. Ayın maaşı öğrencinin
/// ayrıldığı işletmeye yazılıyordu.</para>
/// </summary>
public sealed class PlacementClosurePolicyTests
{
    private static readonly Guid Student = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherStudent = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Business = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OtherBusiness = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly DateTime PlacedOn = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime TerminatedOn = new(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

    private static bool ShouldClose(
        Guid? placementStudent = null,
        Guid? placementBusiness = null,
        bool isActive = true,
        DateTime? placedAt = null)
        => PlacementClosurePolicy.ShouldClose(
            placementStudent ?? Student,
            placementBusiness ?? Business,
            isActive,
            placedAt ?? PlacedOn,
            Student,
            Business,
            TerminatedOn);

    [Fact]
    public void Fesih_ilgili_yerlestirmeyi_kapatir()
    {
        // #152'nin çekirdeği: eskiden bu hiç olmuyordu.
        ShouldClose().ShouldBeTrue();
    }

    [Fact]
    public void Baska_ogrencinin_yerlestirmesi_kapanmaz()
    {
        ShouldClose(placementStudent: OtherStudent).ShouldBeFalse();
    }

    [Fact]
    public void Ayni_ogrencinin_baska_isletmedeki_yerlestirmesi_kapanmaz()
    {
        // Fesih sonrası yeni işletmeye yerleşen öğrencinin yeni kaydı korunmalı — iş kuralı
        // gereği doğrudan transfer yok, fesih + yeni sözleşme var. Bu kayıt kapatılsaydı
        // öğrenci fiilen çalışırken maaşı kesilirdi.
        ShouldClose(placementBusiness: OtherBusiness).ShouldBeFalse();
    }

    [Fact]
    public void Zaten_kapali_yerlestirme_tekrar_kapatilmaz()
    {
        ShouldClose(isActive: false).ShouldBeFalse();
    }

    [Fact]
    public void Fesihten_SONRA_baslamis_yerlestirme_kapanmaz()
    {
        // Aynı işletmeyle yeniden sözleşme (nadir ama mümkün): öğrenci+işletme ikilisi hem
        // eski hem yeni yerleştirmeyi bulur. Feshin ANINDAN SONRA başlayan, o feshin konusu
        // olamaz.
        ShouldClose(placedAt: TerminatedOn.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void Fesihle_ayni_anda_baslamis_yerlestirme_kapanir()
    {
        // Sınır durumu: eşitlik kapatma yönünde. Fesihle aynı damgayı taşıyan kayıt, feshin
        // kendisinden doğmuş olamaz (yeni sözleşme sonradan gelir); eski kayıt sayılır.
        ShouldClose(placedAt: TerminatedOn).ShouldBeTrue();
    }

    [Fact]
    public void PlacedAt_alani_olmayan_eski_kayitlar_kapanir()
    {
        // Bu alan #152 ile eklendi; önceki kayıtlarda DateTime.MinValue olarak deserialize olur.
        // O kayıtlar her fesihten önce sayılır ve kapanırlar — istenen davranış budur, çünkü
        // feshedilen zaten onlardır.
        ShouldClose(placedAt: DateTime.MinValue).ShouldBeTrue();
    }
}
