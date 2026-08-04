using MESNET.Enrollment.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// Projeksiyon yeniden yayınında atlama kuralı (#185).
///
/// <para><b>Neden test:</b> yeni bir read-model eklendiğinde tüketici yalnız <b>bundan sonra</b>
/// gelen olayları yazar; mevcut kayıtlar ancak <c>POST /api/placements/resync-projections</c>
/// ile dolar. O uç, kaynak kaydı eksik olan yerleştirmeleri atlar — çünkü <c>StudentPlaced</c>'i
/// dinleyen dört tüketici adları denormalize tutuyor ve eksik adla yayın onların verisini boş
/// dizeyle ezerdi.</para>
///
/// <para><b>Kilitlenen tuzak:</b> koşul <c>student is null || business is null</c> diye
/// sadeleştirilirse okulda staj (#159) yapan her öğrenci <b>sessizce atlanır</b> — işletmesi
/// zaten yoktur. Sonuç: <c>SchoolPlacedStudentView</c> hiç dolmaz, öğrenci not giriş listesinde
/// görünmez, dönem notu girilemez. #171'in düzelttiği sorun, hiçbir hata vermeden geri gelir.</para>
/// </summary>
public sealed class PlacementResyncPolicyTests
{
    private static readonly Guid BusinessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ── Okulda staj: işletmenin yokluğu eksik veri DEĞİLDİR ─────────────────────────────

    /// <summary><b>Asıl regresyon.</b> İşverensiz yerleştirme yayınlanmalı.</summary>
    [Fact]
    public void Okulda_staj_isletmesi_yok_diye_atlanmaz()
    {
        PlacementResyncPolicy.ShouldSkip(hasStudent: true, businessId: null, hasBusiness: false)
            .ShouldBeFalse("Okulda stajda işletme yoktur; yokluğu atlama sebebi değildir (#159).");
    }

    // ── İşletmeli staj: işletme kaydı eksikse atlanır ──────────────────────────────────

    [Fact]
    public void Isletmeli_stajda_isletme_kaydi_yoksa_atlanir()
    {
        PlacementResyncPolicy.ShouldSkip(hasStudent: true, businessId: BusinessId, hasBusiness: false)
            .ShouldBeTrue("Eksik adla yayın, tüketicilerin BusinessName alanını boş dizeyle ezer.");
    }

    [Fact]
    public void Isletmeli_stajda_her_sey_yerindeyse_atlanmaz()
    {
        PlacementResyncPolicy.ShouldSkip(hasStudent: true, businessId: BusinessId, hasBusiness: true)
            .ShouldBeFalse();
    }

    // ── Öğrenci her hâlde gerekir ──────────────────────────────────────────────────────

    [Fact]
    public void Ogrenci_kaydi_yoksa_isletmeli_staj_atlanir()
    {
        PlacementResyncPolicy.ShouldSkip(hasStudent: false, businessId: BusinessId, hasBusiness: true)
            .ShouldBeTrue();
    }

    /// <summary>
    /// Öğrenci adı türden bağımsız olarak gerekir — okulda stajda da atlanmalı, yoksa
    /// <c>SchoolPlacedStudentView.StudentName</c> boş dizeyle yazılır ve satır kimliksiz kalır.
    /// </summary>
    [Fact]
    public void Ogrenci_kaydi_yoksa_okulda_staj_da_atlanir()
    {
        PlacementResyncPolicy.ShouldSkip(hasStudent: false, businessId: null, hasBusiness: false)
            .ShouldBeTrue();
    }

    /// <summary>
    /// Tutarsız girdi (işletme yok ama işletme kaydı var) atlanmaz — kural işletmenin
    /// <b>varlığını</b> değil, gerekliyken yokluğunu cezalandırır.
    /// </summary>
    [Fact]
    public void Isletmesiz_yerlestirmede_fazladan_isletme_kaydi_atlamaya_yol_acmaz()
    {
        PlacementResyncPolicy.ShouldSkip(hasStudent: true, businessId: null, hasBusiness: true)
            .ShouldBeFalse();
    }
}
