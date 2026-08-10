using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Personel kaydından kullanıcı hesabına backfill kararı (ADR-0003 adım 2.1).
///
/// <para><b>Neden bu adım önce geliyor:</b> kiracı anahtarının otoritesi
/// <c>UserAccount.InstitutionId</c>'dir. Mevcut kullanıcıların çoğunda o alan boş — kurum
/// bilgisi bugüne kadar token claim'inden okunuyordu. Backfill yapılmadan token yolu
/// kapatılırsa mevcut kullanıcılar <b>kapsamsız kalır ve kilitlenir</b>.</para>
/// </summary>
public sealed class StaffAccountBackfillPolicyTests
{
    private static readonly Guid Institution = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherInstitution = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ─── Kurum (kiracı) anahtarı ─────────────────────────────────────────────────────

    [Fact]
    public void Bos_kurum_alani_doldurulur()
    {
        StaffAccountBackfillPolicy.ShouldFillInstitution(Institution, null).ShouldBeTrue();
    }

    /// <summary>Boş Guid de boşluktur — "hiç kurum yok" ile aynı anlama gelir.</summary>
    [Fact]
    public void Bos_guid_de_bosluk_sayilir()
    {
        StaffAccountBackfillPolicy.ShouldFillInstitution(Institution, Guid.Empty).ShouldBeTrue();
    }

    /// <summary>
    /// <b>Üzerine yazılmaz.</b> İdarenin elle girdiği kurum, personel kaydından gelen
    /// tahminle ezilmez — backfill yalnız boşluğu doldurur.
    /// </summary>
    [Fact]
    public void Dolu_kurum_alani_ezilmez()
    {
        StaffAccountBackfillPolicy.ShouldFillInstitution(Institution, OtherInstitution).ShouldBeFalse();
    }

    /// <summary>Kaynak veri boşsa yazılmaz — backfill uydurmaz.</summary>
    [Fact]
    public void Bos_kaynak_kurum_yazilmaz()
    {
        StaffAccountBackfillPolicy.ShouldFillInstitution(Guid.Empty, null).ShouldBeFalse();
    }

    // ─── Alan (branş) kapsamı ────────────────────────────────────────────────────────

    [Fact]
    public void Bos_brans_listesi_doldurulur()
    {
        StaffAccountBackfillPolicy.ShouldFillBranches("EET", []).ShouldBeTrue();
    }

    [Fact]
    public void Dolu_brans_listesi_ezilmez()
    {
        StaffAccountBackfillPolicy.ShouldFillBranches("EET", ["BT"]).ShouldBeFalse();
    }

    /// <summary>
    /// Branşsız personel normaldir (müdür, müdür yardımcısı) — eksik veri değildir, yazılmaz.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bransi_olmayan_personel_icin_yazilmaz(string? branchCode)
    {
        StaffAccountBackfillPolicy.ShouldFillBranches(branchCode, []).ShouldBeFalse();
    }

    // ─── İki boşluk birbirinden bağımsızdır ─────────────────────────────────────────

    /// <summary>
    /// <b>Asıl tuzak.</b> Branşın boş olması kurum backfill'ini engellememelidir. Tüketici
    /// eskiden branş yoksa <b>erken dönüyordu</b>; bu, okul müdürü ve müdür yardımcısının
    /// kiracı anahtarını sessizce doldurulmamış bırakırdı — ve onlar tam da hiçbir alana
    /// bağlı olmayan rollerdir.
    /// </summary>
    [Fact]
    public void Bransi_olmayan_personelin_kurumu_yine_de_doldurulur()
    {
        StaffAccountBackfillPolicy.ShouldFillBranches(null, []).ShouldBeFalse();
        StaffAccountBackfillPolicy.ShouldFillInstitution(Institution, null).ShouldBeTrue();
    }

    /// <summary>Branşı dolu ama kurumu boş olan kullanıcıda yalnız kurum doldurulur.</summary>
    [Fact]
    public void Iki_bosluk_ayri_ayri_degerlendirilir()
    {
        StaffAccountBackfillPolicy.ShouldFillBranches("EET", ["EET"]).ShouldBeFalse();
        StaffAccountBackfillPolicy.ShouldFillInstitution(Institution, null).ShouldBeTrue();
    }
}
