using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Sağlık raporu onay zinciri (#172) — <b>para etkisi onaydan önce doğmaz</b> regresyonu.
///
/// <para>Bulunan açık: rapor eklendiği anda agregada tür <c>HealthReport</c> oluyordu ve o tür
/// ücret kesintisine tabi değildir (business-rules.md §6.2). Onay adımı hiç yoktu; giriş izni
/// işletme yetkilisinde de bulunduğu için ödemeyi yapan taraf kendi kesintisini tek taraflı
/// kaldırabiliyordu.</para>
/// </summary>
public sealed class HealthReportApprovalChainTests
{
    private static readonly Guid AttendanceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StudentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UploaderId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TeacherId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    /// <summary>Mazeretsiz (kesintiye tabi) bir devamsızlık kaydı.</summary>
    private static AttendanceRecord UnexcusedRecord() => AttendanceRecord.Create(
        new AttendanceMarked(
            AttendanceId, StudentId,
            BusinessId: Guid.NewGuid(), InstitutionId: Guid.NewGuid(), AcademicPeriodId: Guid.NewGuid(),
            Date: new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            AbsenceType: nameof(AbsenceType.Unexcused),
            MarkedById: UploaderId,
            InitialStatus: nameof(AttendanceStatus.Pending)));

    private static HealthReportAttached Attached(bool requiresApproval) => new(
        AttendanceId, StudentId, "health-reports/x.pdf",
        new DateTime(2026, 3, 11, 8, 0, 0, DateTimeKind.Utc), UploaderId, requiresApproval);

    [Fact]
    public void Onay_bekleyen_rapor_devamsizlik_turunu_degistirmez()
    {
        var record = UnexcusedRecord().Apply(Attached(requiresApproval: true));

        record.Type.ShouldBe(AbsenceType.Unexcused);
        record.Type.AffectsSalary.ShouldBeTrue("Onaylanmamış rapor ücret kesintisini kaldıramaz.");
        record.EffectiveReportStatus.ShouldBe(HealthReportStatus.Pending);
        record.HealthReportUrl.ShouldNotBeNull();
    }

    [Fact]
    public void Onaylanan_rapor_turu_degistirir_ve_kesintiyi_kaldirir()
    {
        var record = UnexcusedRecord()
            .Apply(Attached(requiresApproval: true))
            .Apply(new HealthReportApproved(
                AttendanceId, StudentId, TeacherId,
                new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc)));

        record.Type.ShouldBe(AbsenceType.HealthReport);
        record.Type.AffectsSalary.ShouldBeFalse();
        record.EffectiveReportStatus.ShouldBe(HealthReportStatus.Approved);
        record.HealthReportReviewedById.ShouldBe(TeacherId);
    }

    [Fact]
    public void Reddedilen_rapor_turu_degistirmez_kesinti_surer()
    {
        var record = UnexcusedRecord()
            .Apply(Attached(requiresApproval: true))
            .Apply(new HealthReportRejected(
                AttendanceId, StudentId, TeacherId,
                new DateTime(2026, 3, 12, 9, 0, 0, DateTimeKind.Utc), "Belge okunaksız."));

        record.Type.ShouldBe(AbsenceType.Unexcused);
        record.Type.AffectsSalary.ShouldBeTrue();
        record.EffectiveReportStatus.ShouldBe(HealthReportStatus.Rejected);
        record.HealthReportRejectionReason.ShouldBe("Belge okunaksız.");
    }

    /// <summary>
    /// Okul tarafı (koordinatör öğretmen, müdür yardımcısı, müdür) doğrudan girer — onay zaten
    /// kendilerinde biter. Karar giriş anında permission'la verilip olaya yazılır.
    /// </summary>
    [Fact]
    public void Okul_tarafinin_girdigi_rapor_onay_beklemez()
    {
        var record = UnexcusedRecord().Apply(Attached(requiresApproval: false));

        record.Type.ShouldBe(AbsenceType.HealthReport);
        record.EffectiveReportStatus.ShouldBe(HealthReportStatus.Approved);
    }

    /// <summary>
    /// #172 öncesi yazılmış <c>HealthReportAttached</c> olaylarında <c>RequiresApproval</c> alanı
    /// yoktur ve <c>false</c> deserialize olur. O kayıtlar eski davranışlarını korur; geçmişe
    /// dönük olarak onaysız duruma düşürülüp ücret kesintisi canlandırılmaz.
    /// </summary>
    [Fact]
    public void Eski_olaylar_eski_davranisini_korur()
    {
        var legacyEvent = new HealthReportAttached(
            AttendanceId, StudentId, "health-reports/eski.pdf",
            new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));

        legacyEvent.RequiresApproval.ShouldBeFalse();
        UnexcusedRecord().Apply(legacyEvent).Type.ShouldBe(AbsenceType.HealthReport);
    }

    /// <summary>Rapor eklenmemiş kayıt <c>None</c>'dur — eski document'larda alan hiç yoktur.</summary>
    [Fact]
    public void Rapor_eklenmemis_kayit_None_durumundadir()
    {
        UnexcusedRecord().EffectiveReportStatus.ShouldBe(HealthReportStatus.None);
        UnexcusedRecord().EffectiveReportStatus.CanReview.ShouldBeFalse();
    }

    [Fact]
    public void Yalniz_onay_bekleyen_rapor_incelenebilir()
    {
        HealthReportStatus.Pending.CanReview.ShouldBeTrue();
        HealthReportStatus.Approved.CanReview.ShouldBeFalse();
        HealthReportStatus.Rejected.CanReview.ShouldBeFalse();
        HealthReportStatus.None.CanReview.ShouldBeFalse();
    }
}
