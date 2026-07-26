using MESNET.Coordination.Core.ReadModels;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// <see cref="CoordinationViewId"/> — koordinasyon satır kimliğinin
/// <c>(BusinessId, BranchCode, AcademicPeriodId)</c> üçlüsünden deterministik üretimi (#114).
/// </summary>
public sealed class CoordinationViewIdTests
{
    private static readonly Guid Business = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherBusiness = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Period = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OtherPeriod = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Ayni_ucluden_her_zaman_ayni_kimlik_uretilir()
    {
        // Given / When — aynı üçlü iki kez hesaplanır
        var first = CoordinationViewId.For(Business, "EET", Period);
        var second = CoordinationViewId.For(Business, "EET", Period);

        // Then — deterministik
        second.ShouldBe(first);
    }

    [Fact]
    public void Farkli_alan_farkli_kimlik_uretir()
    {
        // Given — aynı işletme ve dönem, iki farklı alan (#114'ün çekirdek senaryosu)
        var eet = CoordinationViewId.For(Business, "EET", Period);
        var mtt = CoordinationViewId.For(Business, "MTT", Period);

        // Then — satırlar ayrışır, biri diğerini ezmez
        mtt.ShouldNotBe(eet);
    }

    [Fact]
    public void Farkli_donem_farkli_kimlik_uretir()
    {
        var current = CoordinationViewId.For(Business, "EET", Period);
        var previous = CoordinationViewId.For(Business, "EET", OtherPeriod);

        previous.ShouldNotBe(current);
    }

    [Fact]
    public void Farkli_isletme_farkli_kimlik_uretir()
    {
        var one = CoordinationViewId.For(Business, "EET", Period);
        var other = CoordinationViewId.For(OtherBusiness, "EET", Period);

        other.ShouldNotBe(one);
    }

    [Theory]
    [InlineData("eet")]
    [InlineData("EET")]
    [InlineData(" Eet ")]
    public void Alan_kodu_buyuk_kucuk_harf_ve_bosluktan_etkilenmez(string branchCode)
    {
        // Given — normalize edilmiş referans kimlik
        var expected = CoordinationViewId.For(Business, "EET", Period);

        // When
        var actual = CoordinationViewId.For(Business, branchCode, Period);

        // Then — aynı alan farklı yazımla gelse de aynı satıra düşer
        actual.ShouldBe(expected);
    }

    [Fact]
    public void Bos_alan_kodu_temel_satir_kimligini_verir()
    {
        // Given / When
        var baseId = CoordinationViewId.Base(Business);

        // Then — temel satır = (işletme, boş alan, boş dönem)
        baseId.ShouldBe(CoordinationViewId.For(Business, "", Guid.Empty));
        baseId.ShouldBe(CoordinationViewId.For(Business, null, Guid.Empty));
    }

    [Fact]
    public void Temel_satir_kimligi_alan_satirlariyla_cakismaz()
    {
        var baseId = CoordinationViewId.Base(Business);
        var branchId = CoordinationViewId.For(Business, "EET", Period);

        baseId.ShouldNotBe(branchId);
        baseId.ShouldNotBe(Business);
    }

    [Fact]
    public void Uretilen_deger_bos_guid_olmaz()
    {
        CoordinationViewId.For(Business, "EET", Period).ShouldNotBe(Guid.Empty);
        CoordinationViewId.Base(Business).ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Uretilen_deger_gecerli_bicimli_uuid_dir()
    {
        // Given — RFC 4122 sürüm/varyant bitleri set edilmiş olmalı
        var bytes = CoordinationViewId.For(Business, "EET", Period).ToByteArray(bigEndian: true);

        // Then
        (bytes[6] >> 4).ShouldBe(8);              // sürüm 8 (özel/isimden türetilmiş)
        (bytes[8] & 0xC0).ShouldBe(0x80);         // varyant RFC 4122
    }
}
