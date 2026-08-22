using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Toplu (atomik) saat kaydının kısıt doğrulaması (#117).
///
/// <para>Kusur: saatler işletme başına ayrı HTTP çağrısıyla kaydediliyor ve <b>her çağrı</b>
/// <c>Σ AssignedHours ≤ TotalWorkloadPool</c> kontrolünden geçiyordu. Havuz 40, mevcut
/// A=20 B=10 iken A=10 B=20 dağıtımı uygulanmak istenince B önce kaydedilirse ara toplam
/// 20 (A henüz düşmemiş) + 20 = 40 çıkıyor, bir saat fazlada işlem reddediliyordu. Sonuç
/// çağrı sırasına bağlıydı; kullanıcı kısmi başarı alıyordu.</para>
/// </summary>
public sealed class BranchHoursPolicyTests
{
    private static readonly Guid BusinessA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BusinessB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Teacher = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static BranchHoursChange Change(
        Guid businessId,
        string name,
        int hours,
        bool honorary = false,
        int max = 40,
        Guid? teacherId = null,
        string? teacherName = null) =>
        new(businessId, name, hours, honorary, max, teacherId, teacherName);

    private static BranchHoursValidationInput Input(
        IReadOnlyList<BranchHoursChange> changes,
        int otherBillableHours = 0,
        int? pool = null,
        IReadOnlyDictionary<Guid, int>? otherTeacherHours = null,
        int? maxWeeklyExtraHours = null) =>
        new(changes, otherBillableHours, pool, otherTeacherHours ?? new Dictionary<Guid, int>(), maxWeeklyExtraHours);

    // ── Kabul kriteri 1: sıradan bağımsız yeniden dağıtım ──

    [Fact]
    public void A20_B10_dagitimi_A10_B20_olarak_kaydedilebilir()
    {
        // Given — havuz 40, alanda yalnız A ve B var; ikisi de sette
        var input = Input(
            [
                Change(BusinessA, "A İşletmesi", 10),
                Change(BusinessB, "B İşletmesi", 20),
            ],
            otherBillableHours: 0,
            pool: 40);

        // When
        var violation = BranchHoursPolicy.Validate(input);

        // Then — 10 + 20 = 30 ≤ 40
        violation.ShouldBeNull();
    }

    [Fact]
    public void Ayni_dagitim_ters_sirada_da_kabul_edilir()
    {
        // Given — aynı set, satırların sırası ters. Tekil çağrılarda B önce gidince
        // ara toplam 20 (A henüz düşmemiş) + 20 = 40 çıkıp reddedilebiliyordu.
        var input = Input(
            [
                Change(BusinessB, "B İşletmesi", 20),
                Change(BusinessA, "A İşletmesi", 10),
            ],
            otherBillableHours: 0,
            pool: 40);

        // When
        var violation = BranchHoursPolicy.Validate(input);

        // Then — set birlikte değerlendirildiği için sonuç sıradan bağımsız
        violation.ShouldBeNull();
    }

    [Fact]
    public void Havuzu_tam_dolduran_dagitim_her_iki_sirada_da_gecerli()
    {
        // Given — havuz 40, dağıtım tam 40 (sınır değeri)
        var forward = Input([Change(BusinessA, "A", 20), Change(BusinessB, "B", 20)], pool: 40);
        var reverse = Input([Change(BusinessB, "B", 20), Change(BusinessA, "A", 20)], pool: 40);

        // Then
        BranchHoursPolicy.Validate(forward).ShouldBeNull();
        BranchHoursPolicy.Validate(reverse).ShouldBeNull();
    }

    // ── Kabul kriteri 2: havuz aşımında hiçbir satır yazılmaz, hata konuşur ──

    [Fact]
    public void Havuzu_asan_set_reddedilir_ve_kirilan_kisit_ile_isletmeleri_soyler()
    {
        // Given — havuz 40, set 25 + 20 = 45
        var input = Input(
            [
                Change(BusinessA, "A İşletmesi", 25),
                Change(BusinessB, "B İşletmesi", 20),
            ],
            pool: 40);

        // When
        var violation = BranchHoursPolicy.Validate(input);

        // Then — kısıt türü, toplam, sınır ve setteki işletmeler taşınır
        violation.ShouldNotBeNull();
        violation.Kind.ShouldBe(HoursViolationKind.WorkloadPoolExceeded);
        violation.Attempted.ShouldBe(45);
        violation.Limit.ShouldBe(40);
        violation.AffectedBusinessNames.ShouldBe(["A İşletmesi", "B İşletmesi"]);

        var message = violation.Describe();
        message.ShouldContain("ders yükü havuzunu");
        message.ShouldContain("A İşletmesi");
        message.ShouldContain("B İşletmesi");
        message.ShouldContain("hiçbir satır yazılmadı");
    }

    [Fact]
    public void Havuz_toplamina_degismeyen_satirlar_da_girer()
    {
        // Given — havuz 40; sette olmayan C satırı 30 saat tutuyor, set 15 istiyor
        var input = Input([Change(BusinessA, "A İşletmesi", 15)], otherBillableHours: 30, pool: 40);

        // When
        var violation = BranchHoursPolicy.Validate(input);

        // Then — 30 + 15 = 45 > 40
        violation.ShouldNotBeNull();
        violation.Kind.ShouldBe(HoursViolationKind.WorkloadPoolExceeded);
        violation.Attempted.ShouldBe(45);
    }

    [Fact]
    public void Havuz_yapilandirilmamissa_havuz_kisiti_uygulanmaz()
    {
        // Given — BranchWorkloadConfig yok (tekil uç noktanın "config yoksa erken dön" davranışı)
        var input = Input([Change(BusinessA, "A", 40)], otherBillableHours: 1000, pool: null);

        // Then
        BranchHoursPolicy.Validate(input).ShouldBeNull();
    }

    // ── Kabul kriteri 3: fahri satır aynı komutla kaydedilir, havuza girmez ──

    [Fact]
    public void Fahri_satir_ayni_komutla_kaydedilir_ve_havuz_toplamina_girmez()
    {
        // Given — havuz 40, A ücretli 40 saat + B fahri (girdide eski 20 saat kalmış)
        var input = Input(
            [
                Change(BusinessA, "A İşletmesi", 40),
                Change(BusinessB, "B İşletmesi", 20, honorary: true),
            ],
            pool: 40);

        // When
        var violation = BranchHoursPolicy.Validate(input);

        // Then — fahri satır 0 katkı verir: toplam 40, havuz aşılmaz
        violation.ShouldBeNull();
    }

    [Fact]
    public void Fahri_satir_saat_kisitlarindan_muaftir()
    {
        // Given — fahri satır tavanının üstünde saat taşıyor; niyet zaten "0 saat" (#115)
        var input = Input(
            [Change(BusinessB, "B İşletmesi", 999, honorary: true, max: 8)],
            pool: 40);

        // Then — reddedilmez, saat kaydedende 0'a sabitlenir
        BranchHoursPolicy.Validate(input).ShouldBeNull();
    }

    [Fact]
    public void Tamami_fahriye_cevrilen_set_ogretmen_limitini_tetiklemez()
    {
        // Given — öğretmen zaten limitin üstünde (diğer satırlarından 30), set yalnız fahri
        var input = Input(
            [Change(BusinessA, "A İşletmesi", 0, honorary: true, teacherId: Teacher, teacherName: "Ahmet Yılmaz")],
            otherTeacherHours: new Dictionary<Guid, int> { [Teacher] = 30 },
            maxWeeklyExtraHours: 24);

        // Then — set toplamı yalnızca düşürür; denetlemek haksız ret olurdu (#115)
        BranchHoursPolicy.Validate(input).ShouldBeNull();
    }

    // ── Kabul kriteri 4: satır tavanı aşımı ──

    [Fact]
    public void Tavani_asan_satir_reddedilir_ve_isletmeyi_adiyla_soyler()
    {
        // Given — B'nin mesafe tavanı 8, istenen 9
        var input = Input(
            [
                Change(BusinessA, "A İşletmesi", 5, max: 8),
                Change(BusinessB, "B İşletmesi", 9, max: 8),
            ],
            pool: 100);

        // When
        var violation = BranchHoursPolicy.Validate(input);

        // Then
        violation.ShouldNotBeNull();
        violation.Kind.ShouldBe(HoursViolationKind.AssignedHoursExceedMax);
        violation.BusinessId.ShouldBe(BusinessB);
        violation.BusinessName.ShouldBe("B İşletmesi");
        violation.Attempted.ShouldBe(9);
        violation.Limit.ShouldBe(8);
        violation.Describe().ShouldContain("B İşletmesi");
    }

    [Fact]
    public void Ucretli_satirda_sifir_saat_reddedilir()
    {
        // Given — "0 saat" ücretli satırda anlamsız; fahri işareti için ayrı bayrak var (#115)
        var input = Input([Change(BusinessA, "A İşletmesi", 0)], pool: 40);

        // When
        var violation = BranchHoursPolicy.Validate(input);

        // Then
        violation.ShouldNotBeNull();
        violation.Kind.ShouldBe(HoursViolationKind.InvalidAssignedHours);
        violation.BusinessName.ShouldBe("A İşletmesi");
        violation.Describe().ShouldContain("A İşletmesi");
    }

    [Fact]
    public void Satir_kisiti_havuz_kisitindan_once_bildirilir()
    {
        // Given — hem tavan hem havuz kırılıyor
        var input = Input([Change(BusinessA, "A İşletmesi", 50, max: 8)], pool: 40);

        // Then — kullanıcı önce kendi girdiği satırı düzeltebilsin
        BranchHoursPolicy.Validate(input)!.Kind.ShouldBe(HoursViolationKind.AssignedHoursExceedMax);
    }

    // ── Kabul kriteri 5: öğretmen azami ek ders saati ──

    [Fact]
    public void Ogretmen_azami_ek_ders_saatini_asan_set_reddedilir()
    {
        // Given — öğretmenin değişmeyen satırlarından 20, set 10 daha istiyor, limit 24
        var input = Input(
            [Change(BusinessA, "A İşletmesi", 10, teacherId: Teacher, teacherName: "Ahmet Yılmaz")],
            pool: 100,
            otherTeacherHours: new Dictionary<Guid, int> { [Teacher] = 20 },
            maxWeeklyExtraHours: 24);

        // When
        var violation = BranchHoursPolicy.Validate(input);

        // Then — hangi öğretmen, hangi kısıt, hangi işletme
        violation.ShouldNotBeNull();
        violation.Kind.ShouldBe(HoursViolationKind.TeacherHoursExceedMax);
        violation.TeacherId.ShouldBe(Teacher);
        violation.Attempted.ShouldBe(30);
        violation.Limit.ShouldBe(24);

        var message = violation.Describe();
        message.ShouldContain("Ahmet Yılmaz");
        message.ShouldContain("A İşletmesi");
        message.ShouldContain("hiçbir satır yazılmadı");
    }

    [Fact]
    public void Ayni_ogretmenin_birden_cok_satiri_tek_toplamda_degerlendirilir()
    {
        // Given — iki satır da aynı öğretmende; tekil çağrılarda her biri ayrı ayrı sınırın
        // altında görünüp toplamda limiti aşabiliyordu
        var input = Input(
            [
                Change(BusinessA, "A İşletmesi", 13, teacherId: Teacher, teacherName: "Ahmet Yılmaz"),
                Change(BusinessB, "B İşletmesi", 13, teacherId: Teacher, teacherName: "Ahmet Yılmaz"),
            ],
            pool: 100,
            maxWeeklyExtraHours: 24);

        // When
        var violation = BranchHoursPolicy.Validate(input);

        // Then — 13 + 13 = 26 > 24
        violation.ShouldNotBeNull();
        violation.Kind.ShouldBe(HoursViolationKind.TeacherHoursExceedMax);
        violation.Attempted.ShouldBe(26);
        violation.AffectedBusinessNames.ShouldBe(["A İşletmesi", "B İşletmesi"]);
    }

    [Fact]
    public void Ogretmen_yapilandirmasi_yoksa_ogretmen_kisiti_uygulanmaz()
    {
        // Given — CoordinationConfig yok
        var input = Input(
            [Change(BusinessA, "A", 40, teacherId: Teacher)],
            otherTeacherHours: new Dictionary<Guid, int> { [Teacher] = 1000 },
            maxWeeklyExtraHours: null);

        // Then
        BranchHoursPolicy.Validate(input).ShouldBeNull();
    }

    [Fact]
    public void Ogretmeni_olmayan_satir_ogretmen_kisitina_girmez()
    {
        // Given — henüz atanmamış işletme
        var input = Input([Change(BusinessA, "A", 40, teacherId: null)], maxWeeklyExtraHours: 8);

        // Then
        BranchHoursPolicy.Validate(input).ShouldBeNull();
    }

    // ── Girdi bütünlüğü ──

    [Fact]
    public void Bos_set_kisit_kirmaz()
    {
        BranchHoursPolicy.Validate(Input([], otherBillableHours: 10, pool: 40)).ShouldBeNull();
    }

    [Fact]
    public void Null_girdi_reddedilir()
    {
        Should.Throw<ArgumentNullException>(() => BranchHoursPolicy.Validate(null!));
    }
}
