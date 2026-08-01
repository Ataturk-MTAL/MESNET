using MESNET.Enrollment.Core.Enums;
using MESNET.Enrollment.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// Okulda staj — işverensiz yerleştirmenin temsili (#159).
///
/// <para><b>Kural:</b> staj yeri bulunamayan öğrenci stajını okulda yapar; <b>ücret de devlet
/// katkısı da ödenmez</b>. 3308 Madde 25 ve Geçici Madde 12 aynı istisnayı AYRI AYRI yazıyor,
/// yani biri diğerinin sonucu değil.</para>
///
/// <para><b>Neden #157'deki kamu bayrağı çözüm değil:</b> o yalnız katkıyı sıfırlar, ücret
/// yükümlülüğünü bırakır — dekont beklenir ve ayın 8'inde gecikme uyarısı gider.</para>
/// </summary>
public sealed class PlacementTypePolicyTests
{
    [Fact]
    public void Isletmede_staj_isletme_kimligi_ZORUNLU()
    {
        PlacementTypePolicy.IsConsistent(PlacementType.Business, Guid.NewGuid()).ShouldBeTrue();
        PlacementTypePolicy.IsConsistent(PlacementType.Business, null).ShouldBeFalse();
    }

    [Fact]
    public void Okulda_staj_isletme_kimligi_TASIYAMAZ()
    {
        // Ters yön de hata: işletme varsa sözleşme kurulabilir ve sistem kanuna aykırı
        // biçimde ücret + katkı hesaplar.
        PlacementTypePolicy.IsConsistent(PlacementType.School, null).ShouldBeTrue();
        PlacementTypePolicy.IsConsistent(PlacementType.School, Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void Okulda_staj_karari_isletmenin_YOKLUGUNDAN_okunur()
    {
        // Tek ölçüt BusinessId: diğer modüller Enrollment'ın tür enum'unu bilmeden de doğru
        // davranabilmeli. Modüller arası olaylarda tür yalnız string olarak taşınır.
        PlacementTypePolicy.IsSchoolBased(null).ShouldBeTrue();
        PlacementTypePolicy.IsSchoolBased(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void Tur_bilgisi_UI_icin_Turkce_slug_tasir()
    {
        PlacementType.Business.Slug.ShouldBe("İşletmede");
        PlacementType.School.Slug.ShouldBe("Okulda");
    }

    [Fact]
    public void Tur_adi_backend_iletisimi_icin_Ingilizce_kalir()
    {
        // SmartEnum kuralı: Name = İngilizce (serialize), Slug = Türkçe (UI).
        PlacementType.Business.Name.ShouldBe("Business");
        PlacementType.School.Name.ShouldBe("School");
    }
}
