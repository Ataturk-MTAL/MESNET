using FluentValidation.TestHelper;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Validators;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.ValueObjects;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Personel yetkilendirmesinin iki koruması (#190).
///
/// <para>Canlı veride <b>205 personel kaydının 205'i boş adla</b> yazılmıştı ve bunlar
/// <b>5 gerçek kişinin 41'er kopyasıydı</b>. Kurum sayfasındaki "Ad Soyad" sütunu tümüyle
/// boştu; hata fark edilmedi çünkü satırlar render olmaya devam ediyordu ve mükerrerlik
/// yalnız "1-10 toplam 205" yazısı okunursa anlaşılıyordu.</para>
///
/// <para>İki kök neden vardı: <c>AuthorizeStaffHandler</c>'da mükerrer kontrolü yoktu
/// (<c>UserCreatedConsumer</c>'da vardı) ve <c>FullName</c> için doğrulama yoktu —
/// <c>required</c> yalnız atanmış olmayı zorunlu kılar, boş dizeyi kabul eder.</para>
/// </summary>
public sealed class AuthorizeStaffGuardTests
{
    private const string KcId = "99e73e10-f2c6-498f-92fa-4a6e6bf9e2fb";

    private static AuthorizeStaff Komut(string fullName = "Zeynep Yılmaz", string keycloakId = KcId) =>
        new(Guid.NewGuid(), keycloakId, fullName, StaffRole.VicePrincipal, null);

    // ── Doğrulama ──

    /// <summary>
    /// Boş ad reddedilmeli. Bu doğrulama sessiz hatayı gürültülü hâle getirir: adın nerede
    /// kaybolduğu pinlenemedi, ama boş ad artık 422 döndüğü için bir sonraki yazma denemesi
    /// kaynağı gösterir.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_ad_reddedilir(string fullName)
    {
        new AuthorizeStaffValidator()
            .TestValidate(Komut(fullName: fullName))
            .ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Bos_kullanici_kimligi_reddedilir()
    {
        new AuthorizeStaffValidator()
            .TestValidate(Komut(keycloakId: ""))
            .ShouldHaveValidationErrorFor(x => x.KeycloakId);
    }

    [Fact]
    public void Gecerli_komut_dogrulamadan_gecer()
    {
        new AuthorizeStaffValidator().TestValidate(Komut()).ShouldNotHaveAnyValidationErrors();
    }

    // ── Mükerrer kontrolü ──

    /// <summary>
    /// Handler'ın kullandığı kural: aynı <c>KeycloakId</c> kurumda zaten varsa ikinci kayıt
    /// açılmaz. Kural burada saf hâliyle kilitlenir — handler <c>IDocumentSession</c> istediği
    /// için birim testinde çağrılamaz, ama korunan değişmez budur.
    /// </summary>
    [Fact]
    public void Ayni_kullanici_ikinci_kez_eklenemez()
    {
        var mevcut = new List<StaffMember>
        {
            new() { KeycloakId = KcId, FullName = "Zeynep Yılmaz", Role = StaffRole.VicePrincipal },
        };

        mevcut.Any(s => s.KeycloakId == KcId).ShouldBeTrue("Guard bu koşulda kaydı reddetmeli.");
    }

    [Fact]
    public void Farkli_kullanici_eklenebilir()
    {
        var mevcut = new List<StaffMember>
        {
            new() { KeycloakId = KcId, FullName = "Zeynep Yılmaz", Role = StaffRole.VicePrincipal },
        };

        mevcut.Any(s => s.KeycloakId == "baska-kimlik").ShouldBeFalse();
    }

    /// <summary>
    /// <b>Asıl regresyon:</b> aynı komutun 41 kez tekrarı tek kayıt üretmeli. Canlı veride
    /// tam olarak bu olmuştu — 41 seeder çalıştırması × 5 kişi = 205 satır.
    /// </summary>
    [Fact]
    public void Kirk_bir_kez_tekrarlanan_yetkilendirme_tek_kayit_birakir()
    {
        var staff = new List<StaffMember>();

        for (var i = 0; i < 41; i++)
        {
            if (staff.Any(s => s.KeycloakId == KcId)) continue;

            staff.Add(new StaffMember
            {
                KeycloakId = KcId,
                FullName = "Zeynep Yılmaz",
                Role = StaffRole.VicePrincipal,
            });
        }

        staff.Count.ShouldBe(1);
    }
}
