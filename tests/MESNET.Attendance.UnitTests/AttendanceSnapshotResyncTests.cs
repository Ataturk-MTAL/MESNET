using System.Reflection;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Handlers;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Attendance.UnitTests;

/// <summary>
/// Geçmişe dönük görünüm onarımı (#256) — <b>hüküm doğurmadan</b> yeniden yayın.
///
/// <para><b>Neden ayrı bir olay:</b> onarım için <c>AttendanceMarked</c> yeniden yayınlansaydı
/// <c>CheckAttendanceLimitHandler</c> devamsızlık sınırını yeniden ölçer ve sınır dolmuşsa
/// <b>fesih onay zincirini yeniden başlatırdı</b>. Onarım amaçlı bir işlem hüküm doğuramaz.</para>
///
/// <para><b>Neden bu izin:</b> uç toplu maaş yeniden hesabı tetikleyebilir.
/// <c>attendance:manage</c> işletme rollerinde de vardır — o izinle korunsaydı ödemeyi yapan
/// taraf kendi kesintisini toplu olarak oynatabilirdi (#172 ilkesi).</para>
/// </summary>
public sealed class AttendanceSnapshotResyncTests
{
    /// <summary>
    /// <b>Asıl kilit.</b> Onarım olayının devamsızlık sınırını ölçtüren bir tüketicisi
    /// OLMAMALI — yoksa yeniden yayın fesih zinciri başlatır.
    /// </summary>
    [Fact]
    public void Onarim_olayi_devamsizlik_sinirini_olcturmez()
    {
        var limitHandlers = typeof(CheckAttendanceLimitHandler)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name is "Handle" or "HandleAsync")
            .Select(m => m.GetParameters().FirstOrDefault()?.ParameterType);

        limitHandlers.ShouldNotContain(typeof(AttendanceSnapshotResynced),
            "Onarım yeniden yayını fesih onay zincirini başlatamaz.");
    }

    /// <summary>
    /// Onarım <b>tam durum</b> taşımalı: artımlı olaylar tarih ve tür taşımadığı için yerel
    /// görünüm onlardan onarılamaz.
    /// </summary>
    [Fact]
    public void Onarim_olayi_tam_durum_tasir()
    {
        var alanlar = typeof(AttendanceSnapshotResynced)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        foreach (var beklenen in new[]
                 {
                     "AttendanceId", "StudentId", "BusinessId", "InstitutionId",
                     "AcademicPeriodId", "Date", "AbsenceType", "Status", "IsDeleted"
                 })
        {
            alanlar.ShouldContain(beklenen,
                $"'{beklenen}' olmadan yerel görünüm doğru onarılamaz.");
        }
    }

    /// <summary>
    /// Silinmiş kayıtlar da yayılmalı — tüketicinin yerel satırı silebilmesi için tek yol bu.
    /// Silinmiş devamsızlıktan ücret kesilmeye devam etmiş olabilir.
    /// </summary>
    [Fact]
    public void Handler_silinmis_kayitlari_da_yayar()
    {
        var kaynak = File.ReadAllText(HandlerSourcePath());

        kaynak.Contains("!r.IsDeleted", StringComparison.Ordinal).ShouldBeFalse(
            "Silinmiş kayıtlar süzülürse yerel satırları hiç silinmez ve kesinti sürer.");
    }

    /// <summary>Sonuç, silinmiş kayıt sayısını ayrıca bildirmeli — operatör ne olduğunu görmeli.</summary>
    [Fact]
    public void Sonuc_silinmis_sayisini_bildirir()
    {
        typeof(ResyncAttendanceSnapshotsResult).GetProperty("DeletedCount").ShouldNotBeNull();
    }

    /// <summary>
    /// <b>İzin kilidi.</b> Uç <c>attendance:manage</c> ile korunamaz; o izin işletme
    /// rollerindedir ve uç toplu maaş yeniden hesabı tetikleyebilir.
    /// </summary>
    [Fact]
    public void Uc_isletme_rollerinin_iznini_kullanmaz()
    {
        var kaynak = File.ReadAllText(EndpointSourcePath());
        var satir = kaynak
            .Split('\n')
            .SkipWhile(l => !l.Contains("resync-snapshots", StringComparison.Ordinal))
            .Take(3)
            .Aggregate("", (a, b) => a + b);

        satir.Contains("Attendance.Report", StringComparison.Ordinal).ShouldBeTrue();
        satir.Contains("Attendance.Manage", StringComparison.Ordinal).ShouldBeFalse(
            "attendance:manage işletme rollerinde de var; toplu maaş oynatmaya açık olurdu.");
    }

    /// <summary>Seçilen iznin işletme rollerinde bulunmadığının kanıtı.</summary>
    [Theory]
    [InlineData(MesnetRoles.CompanyManager)]
    [InlineData(MesnetRoles.MasterTrainer)]
    [InlineData(MesnetRoles.CompanyHR)]
    public void Isletme_rolleri_onarim_iznine_sahip_degil(string rol)
    {
        RolePermissionMap.GetPermissionsForRoles([rol])
            .ShouldNotContain(Permissions.Attendance.Report,
                $"{rol} onarımı tetikleyebilseydi kendi kesintisini toplu oynatabilirdi.");
    }

    private static string HandlerSourcePath() => Path.Combine(RepoRoot(),
        "src/Modules/Attendance/MESNET.Attendance.Application/Handlers/ResyncAttendanceSnapshotsHandler.cs");

    private static string EndpointSourcePath() => Path.Combine(RepoRoot(),
        "src/Modules/Attendance/MESNET.Attendance.Api/AttendanceEndpoints.cs");

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
