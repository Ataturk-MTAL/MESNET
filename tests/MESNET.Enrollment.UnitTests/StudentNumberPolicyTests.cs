using MESNET.Enrollment.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// Öğrenci numarası, <c>StudentProfile</c>'ın doğal anahtarının taşıyıcısıdır (#237).
///
/// <para><b>Neden gerekti:</b> <c>StudentProfile</c>'ın tekillik anahtarı <b>hiç yoktu</b> —
/// <c>RegisterStudentHandler</c> her çağrıda <c>Guid.NewGuid()</c> üretip yazıyordu ve
/// Enrollment.Persistence'ta tek bir <c>IsUnique</c> bulunmuyordu. Aynı öğrenci N kez
/// kaydedilirse N satır doğuyor, hiçbir katman itiraz etmiyordu. #204'te ölçüldü:
/// <b>122 öğrenci → 774 kayıt</b>, bazıları 24 kopya. O issue istemci tarafını düzeltti;
/// sunucudaki kök neden kaldı.</para>
///
/// <para><b>Neden normalleştirme kritik:</b> anahtar ham değer üzerine kurulsaydı
/// <c>" 1101"</c> ile <c>"1101"</c> iki ayrı satır olur ve kısıt hiçbir şey korumazdı —
/// <c>TaxNumberPolicy</c>'de (#150) aynı gerekçeyle aynı karar verilmişti.</para>
/// </summary>
public sealed class StudentNumberPolicyTests
{
    [Fact]
    public void Kenar_bosluklari_atilir_yoksa_kisit_bosa_cikar()
    {
        StudentNumberPolicy.Normalize("  1101  ").ShouldBe("1101");
        StudentNumberPolicy.Normalize(" 1101").ShouldBe(StudentNumberPolicy.Normalize("1101"));
    }

    [Fact]
    public void Normal_deger_degismeden_gecer()
    {
        StudentNumberPolicy.Normalize("1101").ShouldBe("1101");
    }

    /// <summary>
    /// Boş değer <c>null</c>'a düşer — "numarası girilmemiş öğrenci" ile "numarası boş dize olan
    /// öğrenci" aynı şeydir ve ikisi de anahtara girmez.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_deger_null_olur(string? value)
    {
        StudentNumberPolicy.Normalize(value).ShouldBeNull();
    }

    /// <summary>
    /// <b>Numara zorunlu DEĞİL.</b> Bugün <c>string?</c> ve doğrulayıcı yalnız uzunluğa bakıyor;
    /// zorunlu kılmak mevcut kayıtları kırardı. Bu yüzden tekillik <b>kısmi</b> uygulanır:
    /// numarası olmayan öğrenciler kısıtın dışındadır ve birbirleriyle çakışmazlar.
    /// </summary>
    [Fact]
    public void Numarasiz_ogrenci_tekillik_kontrolune_girmez()
    {
        StudentNumberPolicy.RequiresUniqueness(null).ShouldBeFalse();
        StudentNumberPolicy.RequiresUniqueness("   ").ShouldBeFalse();
    }

    [Fact]
    public void Numarali_ogrenci_tekillik_kontrolune_girer()
    {
        StudentNumberPolicy.RequiresUniqueness("1101").ShouldBeTrue();
        StudentNumberPolicy.RequiresUniqueness("  1101  ").ShouldBeTrue("Normalleştirme öncesi boşluk aldatmamalı.");
    }
}
