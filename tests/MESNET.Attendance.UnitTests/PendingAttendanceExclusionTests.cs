using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.Policies;
using MESNET.Attendance.Shared.Events;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Onay bekleyen devamsızlık fesih sayacına girmez (#252) — <b>tek taraflı fesih</b> regresyonu.
///
/// <para><b>Bulunan açık:</b> <c>CheckAttendanceLimitHandler</c> sayarken kaydın durumuna hiç
/// bakmıyordu. İşletme yetkilisi, işletme İK ve usta öğretici devamsızlığı <i>bildirir</i>
/// (<c>attendance:upload</c>) ve girdiği kayıt <c>Pending</c> doğar; koordinatör öğretmen hiç
/// dokunmadan bu kayıtlar eşiği doldurup fesih onay zincirini başlatabiliyordu. Ödemeyi yapan
/// taraf kendi kesintisini tek taraflı kaldıramazken (#172, #177), sözleşmenin feshini tek
/// taraflı başlatabiliyordu.</para>
///
/// <para><b>Ters yön de kilitli:</b> onaylanan kayıt sayaca <b>girer</b>. Süre dolunca
/// kendiliğinden onay yoktur (<c>AutoApproveExpiredAttendance</c> belgede var, kodda yok), yani
/// onay hiç sayılmasaydı mevzuatın emrettiği fesih sessizce hiç tetiklenmezdi.</para>
/// </summary>
public sealed class PendingAttendanceExclusionTests
{
    private static readonly Guid Ogrenci = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Isletme = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Kurum = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Donem = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Ogretmen = Guid.Parse("55555555-5555-5555-5555-555555555555");

    /// <summary>Örgün öğrencide mazeretsiz ayak; eşik <c>FormalUnexcusedDayLimit</c> = 10.</summary>
    private const string Orgun = "Formal";

    private static AttendanceRecord Kayit(
        int gun, string tur = nameof(AbsenceType.Unexcused), string durum = nameof(AttendanceStatus.Pending))
        => AttendanceRecord.Create(new AttendanceMarked(
            AttendanceId: Guid.NewGuid(),
            StudentId: Ogrenci,
            BusinessId: Isletme,
            InstitutionId: Kurum,
            AcademicPeriodId: Donem,
            Date: new DateTime(2026, 3, gun, 0, 0, 0, DateTimeKind.Utc),
            AbsenceType: tur,
            MarkedById: Guid.NewGuid(),
            InitialStatus: durum));

    private static AttendanceRecord Onayla(AttendanceRecord kayit) => kayit.Apply(
        new AttendanceApproved(kayit.Id, kayit.StudentId, Ogretmen,
            new DateTime(2026, 3, 20, 9, 0, 0, DateTimeKind.Utc)));

    private static IEnumerable<AttendanceRecord> Kayitlar(
        int adet, string tur = nameof(AbsenceType.Unexcused), string durum = nameof(AttendanceStatus.Pending))
        => Enumerable.Range(1, adet).Select(gun => Kayit(gun, tur, durum));

    // ─── Sayım: onay bekleyen kayıt hiçbir ayağa yazılmaz ────────────────────────────

    /// <summary><b>Asıl regresyon.</b> Onay bekleyen kayıt ne toplam ne mazeretsiz ayağa girer.</summary>
    [Fact]
    public void Onay_bekleyen_kayit_sayaca_girmez()
    {
        var tally = AttendanceCounterScope.Tally(Kayitlar(5));

        tally.TotalDays.ShouldBe(0, "Onay bekleyen kayıt toplam ayağa yazılamaz.");
        tally.UnexcusedDays.ShouldBe(0, "Onay bekleyen kayıt mazeretsiz ayağa yazılamaz.");
    }

    /// <summary>
    /// <b>Açığın kendisi.</b> İşletmenin tek başına girdiği 12 mazeretsiz gün, 10 günlük
    /// mazeretsiz eşiği <b>dolduramaz</b> — fesih onay zinciri başlamaz.
    /// </summary>
    [Fact]
    public void Isletmenin_tek_tarafli_girisi_fesih_esigini_dolduramaz()
    {
        var tally = AttendanceCounterScope.Tally(Kayitlar(12));

        AttendanceLimitPolicy.Evaluate(Orgun, tally.UnexcusedDays, tally.TotalDays)
            .IsExceeded.ShouldBeFalse("İşletme, öğretmen onayı olmadan feshi tetikleyemez.");
    }

    /// <summary>
    /// <b>Ters yön.</b> Aynı 12 kayıt öğretmence onaylanınca eşik dolar. Bu test olmadan
    /// düzeltme, açığı kapatırken fesih zincirini tümden öldürmüş olurdu.
    /// </summary>
    [Fact]
    public void Onaylanan_kayit_sayaca_girer_ve_esigi_doldurur()
    {
        var tally = AttendanceCounterScope.Tally(Kayitlar(12).Select(Onayla));

        tally.UnexcusedDays.ShouldBe(12);
        AttendanceLimitPolicy.Evaluate(Orgun, tally.UnexcusedDays, tally.TotalDays)
            .IsExceeded.ShouldBeTrue("Onaylanan devamsızlık mevzuatın öngördüğü feshi tetiklemeli.");
    }

    /// <summary>Okul tarafının doğrudan girdiği kayıt (<c>attendance:direct-entry</c>) beklemez.</summary>
    [Fact]
    public void Okul_tarafinin_dogrudan_girdigi_kayit_hemen_sayilir()
    {
        var tally = AttendanceCounterScope.Tally(
            Kayitlar(10, durum: nameof(AttendanceStatus.Recorded)));

        tally.UnexcusedDays.ShouldBe(10, "Okul yolunun onay beklemesi gerekmez.");
    }

    // ─── Kara liste: Pending dışındaki her durum sayılır ─────────────────────────────

    [Theory]
    [InlineData(nameof(AttendanceStatus.Recorded))]
    [InlineData(nameof(AttendanceStatus.Verified))]
    [InlineData(nameof(AttendanceStatus.Corrected))]
    public void Pending_disindaki_durumlar_sayilir(string durum)
    {
        AttendanceCounterScope.CountsTowardLimit(durum)
            .ShouldBeTrue($"'{durum}' hükmü doğmuş bir kayıttır, sayaçtan düşemez.");
    }

    [Fact]
    public void Pending_sayilmaz()
    {
        AttendanceCounterScope.CountsTowardLimit(nameof(AttendanceStatus.Pending)).ShouldBeFalse();
    }

    /// <summary>
    /// Eksik veri sınırı <b>gevşetemez</b> — <c>AttendanceLimitPolicy</c> ile aynı yön.
    /// Durumu okunamayan eski kayıt sayılır, sessizce düşmez.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bilinmeyen")]
    public void Bilinmeyen_durum_sayilir(string? durum)
    {
        AttendanceCounterScope.CountsTowardLimit(durum).ShouldBeTrue();
    }

    // ─── Diğer eksenler korunuyor ────────────────────────────────────────────────────

    /// <summary>Durum ekseni eklenirken silinme ekseni kaybolmamalı.</summary>
    [Fact]
    public void Silinen_kayit_sayilmaz()
    {
        var silinen = Onayla(Kayit(1)).Apply(new AttendanceDeleted(
            Guid.NewGuid(), Ogrenci, "yanlış giriş",
            new DateTime(2026, 3, 21, 0, 0, 0, DateTimeKind.Utc)));

        AttendanceCounterScope.Tally([silinen]).TotalDays.ShouldBe(0);
    }

    /// <summary>
    /// Toplam ayak (örgünde 30 gün) mazeretli günleri de sayar ama onay bekleyeni saymaz —
    /// iki eksen birbirine karışmamalı.
    /// </summary>
    [Fact]
    public void Onay_bekleyen_mazeretli_kayit_toplam_ayaga_da_girmez()
    {
        var tally = AttendanceCounterScope.Tally(Kayitlar(31, tur: nameof(AbsenceType.Excused)));

        tally.TotalDays.ShouldBe(0);
        AttendanceLimitPolicy.Evaluate(Orgun, tally.UnexcusedDays, tally.TotalDays)
            .IsExceeded.ShouldBeFalse();
    }

    /// <summary>Karışık küme: yalnız onaylananlar sayılır.</summary>
    [Fact]
    public void Karisik_kumede_yalniz_onaylananlar_sayilir()
    {
        var kayitlar = Kayitlar(4).Select(Onayla)
            .Concat(Kayitlar(6))
            .ToList();

        AttendanceCounterScope.Tally(kayitlar).UnexcusedDays.ShouldBe(4);
    }
}
