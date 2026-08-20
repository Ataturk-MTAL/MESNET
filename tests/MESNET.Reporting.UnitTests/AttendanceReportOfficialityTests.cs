using System.Reflection;
using MESNET.Attendance.Shared.Events;
using MESNET.Reporting.Application.Consumers;
using MESNET.Reporting.Core.ReadModels;
using Shouldly;
using Xunit;

namespace MESNET.Reporting.UnitTests;

/// <summary>
/// Resmî aylık devamsızlık formu <b>yalnız hükmü doğmuş</b> kayıtları gösterir (#257).
///
/// <para><b>Bulunan iki kusur:</b> (1) form onay bekleyen (<c>Pending</c>) kayıtları devamsızlık
/// sayıyordu — işletmenin tek taraflı bildirimi, koordinatör öğretmen onaylamadan velinin ve
/// idarenin gördüğü belgede <c>D</c> sembolüyle görünüyordu; (2) <c>AttendanceCorrected</c> ve
/// <c>AttendanceDeleted</c> tüketicileri <b>boş no-op</b>'tu, yani yanlış girilip düzeltilen ya
/// da silinen devamsızlık formda <b>kalıcı</b>ydı.</para>
///
/// <para>İkinci kusurun sebebi olayların tarih taşımamasıydı; satıra <c>AttendanceId</c>
/// eklenerek kayıt tarihe ihtiyaç duymadan bulunur hâle geldi.</para>
/// </summary>
public sealed class AttendanceReportOfficialityTests
{
    private static readonly Guid Kayit = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // ─── Resmîlik yüklemi ────────────────────────────────────────────────────────────

    /// <summary><b>Asıl regresyon.</b> Onay bekleyen kayıt resmî forma yazılmaz.</summary>
    [Fact]
    public void Onay_bekleyen_kayit_resmi_forma_yazilmaz()
    {
        new AbsentDayEntry(3, "Unexcused", Kayit, "Pending").IsOfficial
            .ShouldBeFalse("İşletmenin tek taraflı bildirimi resmî belgede devamsızlık sayılamaz.");
    }

    [Theory]
    [InlineData("Recorded")]
    [InlineData("Verified")]
    [InlineData("Corrected")]
    public void Hukmu_dogmus_kayitlar_yazilir(string durum)
    {
        new AbsentDayEntry(3, "Unexcused", Kayit, durum).IsOfficial.ShouldBeTrue();
    }

    /// <summary>
    /// <b>Bilinmeyen durum GÖSTERİLİR.</b> Alan eklenmeden önce yazılmış satırlarda değer yoktur;
    /// onları gizlemek var olan formlardan veri silmek olurdu. Onarım resync ile yapılır (#256).
    /// </summary>
    [Fact]
    public void Durumu_bilinmeyen_eski_satir_gosterilir()
    {
        new AbsentDayEntry(3, "Unexcused").IsOfficial
            .ShouldBeTrue("Eski satırları gizlemek var olan formdan veri silmek olurdu.");
    }

    // ─── Geriye uyumluluk ────────────────────────────────────────────────────────────

    /// <summary>
    /// Yeni alanlar <b>sona</b> ve <b>varsayılanlı</b> eklendi: bu alanlardan önce yazılmış
    /// belgeler bozulmadan deserialize olmalı.
    /// </summary>
    [Fact]
    public void Yeni_alanlar_varsayilanli_ve_sonda()
    {
        var ctor = typeof(AbsentDayEntry).GetConstructors().Single();
        var parametreler = ctor.GetParameters();

        parametreler[0].Name.ShouldBe("Day");
        parametreler[1].Name.ShouldBe("AbsenceType");

        parametreler.Single(p => p.Name == "AttendanceId").HasDefaultValue.ShouldBeTrue();
        parametreler.Single(p => p.Name == "StatusName").HasDefaultValue.ShouldBeTrue();
    }

    // ─── Yaşam döngüsü tüketicileri ──────────────────────────────────────────────────

    /// <summary>
    /// Kaydın hâlini değiştiren her olayın tüketicisi olmalı — düzeltme ve silme artık
    /// boş no-op olamaz.
    /// </summary>
    [Theory]
    [InlineData(typeof(AttendanceMarked))]
    [InlineData(typeof(AttendanceApproved))]
    [InlineData(typeof(AttendanceVerified))]
    [InlineData(typeof(AttendanceCorrected))]
    [InlineData(typeof(AttendanceDeleted))]
    [InlineData(typeof(HealthReportApproved))]
    [InlineData(typeof(HealthReportAttached))]
    [InlineData(typeof(AttendanceSnapshotResynced))]
    public void Kaydin_halini_degistiren_olay_tuketiliyor(Type olayTipi)
    {
        HandlerFor(olayTipi).ShouldNotBeNull($"{olayTipi.Name} için tüketici yok.");
    }

    /// <summary>
    /// <b>Boş no-op kilidi.</b> Düzeltme ve silme tüketicileri gerçekten iş yapmalı; gövdesi boş
    /// bırakılırsa form yine bozuk kalır ve bunu hiçbir şey söylemez.
    /// </summary>
    [Fact]
    public void Duzeltme_ve_silme_bos_no_op_degil()
    {
        var kaynak = File.ReadAllText(ConsumerSourcePath());

        kaynak.Contains("// AttendanceCorrected'da tarih bilgisi yok", StringComparison.Ordinal)
            .ShouldBeFalse("Düzeltme tüketicisi hâlâ boş no-op.");
        kaynak.Contains("// AttendanceDeleted'da tarih bilgisi yok", StringComparison.Ordinal)
            .ShouldBeFalse("Silme tüketicisi hâlâ boş no-op.");
        kaynak.Contains("RemoveAt", StringComparison.Ordinal)
            .ShouldBeTrue("Silme, satırı formdan gerçekten düşürmeli.");
    }

    // ─── Rapor üretimi: davranış testi ───────────────────────────────────────────────

    /// <summary>
    /// <b>Asıl davranış kilidi.</b> Onay bekleyen gün forma <b>hiç yazılmaz</b>: ne sembol
    /// satırına, ne de mazeretsiz/mazeretli sayacına.
    ///
    /// <para>Rapor satırını kuran metot private; reflection'la çağrılıyor. Yüklem testinin
    /// (<see cref="Onay_bekleyen_kayit_resmi_forma_yazilmaz"/>) tek başına yetmediği ölçüldü:
    /// üretimdeki süzgeç kaldırıldığında o test yeşil kalıyordu.</para>
    /// </summary>
    [Fact]
    public void Onay_bekleyen_gun_form_satirina_girmez()
    {
        var satir = SatirKur(
            new AbsentDayEntry(3, "Unexcused", Guid.NewGuid(), "Pending"),
            new AbsentDayEntry(7, "Unexcused", Guid.NewGuid(), "Recorded"));

        var sabahIsaretleri = (IDictionary<int, string?>)Oku(satir, "MorningMarks")!;

        sabahIsaretleri.ContainsKey(3).ShouldBeFalse("Onay bekleyen gün formda görünemez.");
        sabahIsaretleri.ContainsKey(7).ShouldBeTrue("Onaylanmış gün formda görünmeli.");

        Oku(satir, "UnexcusedAbsences").ShouldBe(1, "Onay bekleyen gün sayaca da girmemeli.");
    }

    /// <summary>Durumu bilinmeyen eski satır formda kalır — veri silinmez.</summary>
    [Fact]
    public void Eski_satir_formda_kalir()
    {
        var satir = SatirKur(new AbsentDayEntry(5, "Unexcused"));

        ((IDictionary<int, string?>)Oku(satir, "MorningMarks")!).ContainsKey(5).ShouldBeTrue();
    }

    private static object SatirKur(params AbsentDayEntry[] gunler)
    {
        var handler = typeof(AttendanceReportConsumer).Assembly
            .GetType("MESNET.Reporting.Application.Handlers.GenerateMonthlyAttendanceReportHandler")!;

        var metot = handler.GetMethod("BuildStudentRow",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var yerlestirme = Activator.CreateInstance(metot.GetParameters()[0].ParameterType)!;

        var devamsizlik = new StudentAttendanceReportView
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            Year = 2026,
            Month = 3,
            AbsentDays = [.. gunler]
        };

        return metot.Invoke(null, [yerlestirme, devamsizlik])!;
    }

    private static object? Oku(object satir, string alan) =>
        satir.GetType().GetProperty(alan)!.GetValue(satir);

    private static MethodInfo? HandlerFor(Type olayTipi) => typeof(AttendanceReportConsumer)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.Name is "Consume" or "ConsumeAsync" or "Handle" or "HandleAsync")
        .FirstOrDefault(m => m.GetParameters().FirstOrDefault()?.ParameterType == olayTipi);

    private static string ConsumerSourcePath() => Path.Combine(RepoRoot(),
        "src/Modules/Reporting/MESNET.Reporting.Application/Consumers/AttendanceReportConsumer.cs");

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx).");
    }
}
