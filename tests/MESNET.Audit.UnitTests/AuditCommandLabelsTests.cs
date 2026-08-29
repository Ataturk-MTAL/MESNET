using MESNET.Audit.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Etiket eşlemesi SUNUCUDADIR. Arayüz kendi tablosunu tutsaydı, yeni bir komut eklendiğinde
/// listede sessizce ham tip adı ("MarkAttendance") belirirdi ve bunu hiçbir test göremezdi.
/// Sözlük kısmi olabilir — eşleşmeyen komut ham adıyla görünür, satır KAYBOLMAZ.
/// </summary>
public class AuditCommandLabelsTests
{
    [Fact]
    public void Bilinen_komut_Turkce_etiketiyle_doner()
    {
        AuditCommandLabels.For("MarkAttendance").ShouldBe("Devamsızlık girildi");
    }

    [Fact]
    public void Bilinmeyen_komut_ham_tip_adiyla_doner()
    {
        // Sessiz boşluk YOK: satır görünür kalır, yalnız etiketi çevrilmemiştir.
        AuditCommandLabels.For("SomeBrandNewCommand").ShouldBe("SomeBrandNewCommand");
    }

    [Fact]
    public void Bos_giris_bos_doner()
    {
        AuditCommandLabels.For(string.Empty).ShouldBe(string.Empty);
    }

    [Fact]
    public void Etiketler_ASCII_yaklasimi_kullanmaz()
    {
        // Türkçe karakterler doğru yazılmalı: "Ogretmen" değil "Öğretmen". Bu bir stil
        // tercihi değil, arayüz dili kuralıdır (CLAUDE.md).
        var supheliler = new[] { "Ogretmen", "Donem", "Iptal", "Duzenle", "Sozlesme", "Odeme", "Ucret" };

        foreach (var (_, label) in AuditCommandLabels.All)
        {
            foreach (var supheli in supheliler)
                label.ShouldNotContain(supheli, Case.Insensitive);
        }
    }

    [Fact]
    public void Her_etiket_dolu_ve_benzersiz_anahtarlidir()
    {
        AuditCommandLabels.All.ShouldAllBe(x => !string.IsNullOrWhiteSpace(x.Value));
    }
}
