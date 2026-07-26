using MESNET.Coordination.Core.ReadModels;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Fahri (ücretsiz) ziyaret muhasebesi (#115).
///
/// <para>Kusur: <c>AssignedHours == 0</c> hem "henüz takdir edilmedi" hem "fahri" anlamına
/// geliyordu ve her iki durumda da <c>AssignedHours &gt; 0 ? AssignedHours : MaxCoordinationHours</c>
/// fallback'i devreye girip 0 saati sessizce mesafe TAVANINA çeviriyordu. Fahri işletme
/// öğretmene atanırken 8 saat tüketiyormuş gibi sayılıyor, havuz ve öğretmen kapasitesi
/// şişiyordu.</para>
/// </summary>
public sealed class HonoraryVisitTests
{
    private const int MaxHours = 8;

    private static BusinessCoordinationView Row(int assignedHours, bool isHonorary) => new()
    {
        Id = CoordinationViewId.For(Guid.NewGuid(), "EET", Guid.NewGuid()),
        MaxCoordinationHours = MaxHours,
        AssignedHours = assignedHours,
        IsHonoraryVisit = isHonorary,
    };

    // ── Üç durumun ayrışması ──

    [Fact]
    public void Takdir_edilmemis_satir_ucret_dogurmaz_ama_hedef_saat_tavana_duser()
    {
        // Given — 0 saat, fahri DEĞİL: "henüz takdir edilmedi"
        var row = Row(assignedHours: 0, isHonorary: false);

        // Then — mevcut davranış korunur: hedef saat mesafe tavanı
        row.BillableHours().ShouldBe(0);
        row.BillableTargetHours().ShouldBe(MaxHours);
        row.SlotTargetHours().ShouldBe(MaxHours);
    }

    [Fact]
    public void Fahri_satir_tavana_dusmez()
    {
        // Given — 0 saat + fahri işareti
        var row = Row(assignedHours: 0, isHonorary: true);

        // Then — issue #115'in düzelttiği asıl kusur: 8 saat DEĞİL, 0
        row.BillableHours().ShouldBe(0);
        row.BillableTargetHours().ShouldBe(0);
    }

    [Fact]
    public void Fahri_satir_ders_programinda_slot_isgal_eder()
    {
        // Given
        var row = Row(assignedHours: 0, isHonorary: true);

        // Then — ziyaret yapılıyor: ücret doğurmaması slot işgal etmediği anlamına gelmez
        row.SlotTargetHours().ShouldBe(BusinessCoordinationView.HonoraryVisitSlots);
        row.SlotTargetHours().ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Ucretli_satirda_takdir_edilen_saat_aynen_gecerlidir()
    {
        // Given
        var row = Row(assignedHours: 5, isHonorary: false);

        // Then
        row.BillableHours().ShouldBe(5);
        row.BillableTargetHours().ShouldBe(5);
        row.SlotTargetHours().ShouldBe(5);
    }

    [Fact]
    public void Eksik_alan_tasiyan_eski_kayit_fahri_sayilmaz()
    {
        // Given — Marten JSONB: #115 öncesi kayıtlarda alan yok, C# varsayılanı false
        var legacy = new BusinessCoordinationView { MaxCoordinationHours = MaxHours };

        // Then — eski davranış aynen sürer
        legacy.IsHonoraryVisit.ShouldBeFalse();
        legacy.BillableTargetHours().ShouldBe(MaxHours);
    }

    // ── Toplamlar ──

    [Fact]
    public void Havuz_toplami_fahri_satirlari_saymaz()
    {
        // Given — 5 ücretli + 2 fahri + 1 takdir edilmemiş
        var rows = new[]
        {
            Row(assignedHours: 5, isHonorary: false),
            Row(assignedHours: 0, isHonorary: true),
            Row(assignedHours: 0, isHonorary: true),
            Row(assignedHours: 0, isHonorary: false),
        };

        // When
        var poolTotal = rows.Sum(r => r.BillableHours());

        // Then — yalnız takdir edilmiş ücretli saat
        poolTotal.ShouldBe(5);
    }

    [Fact]
    public void Ogretmen_kapasitesi_toplaminda_fahri_satir_tavan_tuketmez()
    {
        // Given — öğretmenin işletmeleri: 3 saat ücretli + fahri + takdir edilmemiş
        var teacherRows = new[]
        {
            Row(assignedHours: 3, isHonorary: false),
            Row(assignedHours: 0, isHonorary: true),
            Row(assignedHours: 0, isHonorary: false),
        };

        // When
        var teacherHours = teacherRows.Sum(r => r.BillableTargetHours());

        // Then — 3 (ücretli) + 0 (fahri) + 8 (takdir edilmemiş → tavan) = 11
        // Kusurlu hâlde fahri satır da 8 sayılıp 19 çıkıyordu.
        teacherHours.ShouldBe(11);
    }

    [Fact]
    public void Fahri_satirin_slotlari_ek_ders_kotasindan_dusulmez()
    {
        // Given — biri ücretli 2 slot, biri fahri 1 slot
        var paid = Row(assignedHours: 2, isHonorary: false);
        paid.AssignedSlots.Add(new AssignedSlotInfo("Monday", 1));
        paid.AssignedSlots.Add(new AssignedSlotInfo("Monday", 2));

        var honorary = Row(assignedHours: 0, isHonorary: true);
        honorary.AssignedSlots.Add(new AssignedSlotInfo("Tuesday", 3));

        var rows = new[] { paid, honorary };

        // When — ek ders kotası (AssignBusinessToTeacherHandler kuralı)
        var quotaSlots = rows.Where(r => !r.IsHonoraryVisit).Sum(r => r.AssignedSlots.Count);
        // ders programındaki gerçek işgal
        var occupiedSlots = rows.Sum(r => r.AssignedSlots.Count);

        // Then — ikisi ayrışır: kota 2, program işgali 3
        quotaSlots.ShouldBe(2);
        occupiedSlots.ShouldBe(3);
    }
}
