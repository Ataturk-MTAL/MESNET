using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Attendance olaylarından resmî aylık devamsızlık formunun veri kaynağını besler.
///
/// <para><b>Düzeltme ve silme artık uygulanıyor (#257).</b> Önceden ikisi de <b>boş no-op</b>'tu:
/// olaylar tarih taşımadığı için hangi günün güncelleneceği bilinemiyordu ve yanlış girilip
/// düzeltilen ya da silinen devamsızlık resmî formda <b>kalıcı</b> hâle geliyordu. Satıra
/// <c>AttendanceId</c> eklenince kayıt tarihe ihtiyaç duymadan bulunabiliyor.</para>
///
/// <para><b>Neden olaya tarih eklenmedi:</b> bu olaylar <c>shared.mt_events</c> içinde kalıcıdır
/// ve saklı olayların şekli değiştirilemez; sona varsayılanlı alan eklemek de yalnız <b>yeni</b>
/// olayları düzeltirdi. Kimliği görünümde tutmak geçmişi de onarılabilir kılar
/// (<c>AttendanceSnapshotResynced</c>, #256).</para>
///
/// <para><b>Arama öğrenci başına yapılır:</b> bir öğrencinin form satırları ay başına tek
/// belgedir, yani küme küçüktür. <c>AttendanceId</c>'yi JSON dizisi içinde LINQ'le aramak yerine
/// öğrencinin belgeleri çekilip bellekte eşleştirilir.</para>
/// </summary>
public static class AttendanceReportConsumer
{
    public static async Task Consume(AttendanceMarked @event, IDocumentSession session)
    {
        var view = await FindOrCreate(session, @event.StudentId, @event.InstitutionId,
            @event.AcademicPeriodId, @event.Date.Year, @event.Date.Month);

        Upsert(view, new AbsentDayEntry(
            @event.Date.Day, @event.AbsenceType, @event.AttendanceId, @event.InitialStatus));

        session.Store(view);
    }

    /// <summary>Onay: kayıt resmî forma <b>bu anda</b> girer (#257).</summary>
    public static Task Consume(AttendanceApproved @event, IDocumentSession session)
        => UpdateEntryAsync(session, @event.StudentId, @event.AttendanceId,
            entry => entry with { StatusName = "Recorded" });

    public static Task Consume(AttendanceVerified @event, IDocumentSession session)
        => UpdateEntryAsync(session, @event.StudentId, @event.AttendanceId,
            entry => entry with { StatusName = "Verified" });

    public static Task Consume(AttendanceCorrected @event, IDocumentSession session)
        => UpdateEntryAsync(session, @event.StudentId, @event.AttendanceId,
            entry => entry with { AbsenceType = @event.NewAbsenceType, StatusName = "Corrected" });

    public static Task Consume(HealthReportApproved @event, IDocumentSession session)
        => UpdateEntryAsync(session, @event.StudentId, @event.AttendanceId,
            entry => entry with { AbsenceType = "HealthReport" });

    /// <summary>
    /// Rapor yüklendi (#172). Tür YALNIZ okul tarafının doğrudan girdiği raporda değişir
    /// (<c>RequiresApproval = false</c>); onay bekleyen yüklemede forma dokunulmaz.
    /// </summary>
    public static Task Consume(HealthReportAttached @event, IDocumentSession session)
        => @event.RequiresApproval
            ? Task.CompletedTask
            : UpdateEntryAsync(session, @event.StudentId, @event.AttendanceId,
                entry => entry with { AbsenceType = "HealthReport" });

    /// <summary>Silinen devamsızlık resmî formdan <b>düşer</b>.</summary>
    public static async Task Consume(AttendanceDeleted @event, IDocumentSession session)
    {
        var (view, index) = await FindEntryAsync(session, @event.StudentId, @event.AttendanceId);
        if (view is null) return;

        view.AbsentDays.RemoveAt(index);
        session.Store(view);
    }

    /// <summary>
    /// Geçmişe dönük onarım (#256/#257): kaydın bugünkü tam hâli gelir. Kimliği ya da durumu
    /// olmayan eski satırlar ancak bu yolla düzelir — dağıtım ön koşuludur.
    /// </summary>
    public static async Task Consume(AttendanceSnapshotResynced @event, IDocumentSession session)
    {
        // Kayıt taşınmış olabilir (tarih düzeltmesi): önce eski satırdan temizle.
        var (existing, index) = await FindEntryAsync(session, @event.StudentId, @event.AttendanceId);
        if (existing is not null)
        {
            existing.AbsentDays.RemoveAt(index);
            session.Store(existing);
        }

        if (@event.IsDeleted) return;

        var view = await FindOrCreate(session, @event.StudentId, @event.InstitutionId,
            @event.AcademicPeriodId, @event.Date.Year, @event.Date.Month);

        Upsert(view, new AbsentDayEntry(
            @event.Date.Day, @event.AbsenceType, @event.AttendanceId, @event.Status));

        session.Store(view);
    }

    /// <summary>Aynı gün için ikinci satır açılmaz — kimlik varsa ona, yoksa güne göre eşleşir.</summary>
    private static void Upsert(StudentAttendanceReportView view, AbsentDayEntry entry)
    {
        var index = view.AbsentDays.FindIndex(a =>
            (entry.AttendanceId != default && a.AttendanceId == entry.AttendanceId)
            || a.Day == entry.Day);

        if (index >= 0)
            view.AbsentDays[index] = entry;
        else
            view.AbsentDays.Add(entry);
    }

    private static async Task UpdateEntryAsync(
        IDocumentSession session, Guid studentId, Guid attendanceId,
        Func<AbsentDayEntry, AbsentDayEntry> apply)
    {
        var (view, index) = await FindEntryAsync(session, studentId, attendanceId);
        if (view is null) return;

        view.AbsentDays[index] = apply(view.AbsentDays[index]);
        session.Store(view);
    }

    /// <summary>
    /// Kaydı öğrencinin form belgeleri arasında arar. <c>AttendanceId</c> taşımayan eski satırlar
    /// bulunamaz — onlar <c>AttendanceSnapshotResynced</c> ile onarılır.
    /// </summary>
    private static async Task<(StudentAttendanceReportView? View, int Index)> FindEntryAsync(
        IDocumentSession session, Guid studentId, Guid attendanceId)
    {
        if (attendanceId == default) return (null, -1);

        var views = await session.Query<StudentAttendanceReportView>()
            .Where(v => v.StudentId == studentId)
            .ToListAsync();

        foreach (var view in views)
        {
            var index = view.AbsentDays.FindIndex(a => a.AttendanceId == attendanceId);
            if (index >= 0) return (view, index);
        }

        return (null, -1);
    }

    private static async Task<StudentAttendanceReportView> FindOrCreate(
        IDocumentSession session, Guid studentId, Guid institutionId,
        Guid academicPeriodId, int year, int month)
    {
        // Deterministic ID: StudentId + Year + Month
        var id = GenerateDeterministicId(studentId, year, month);

        var view = await session.LoadAsync<StudentAttendanceReportView>(id);
        if (view is not null) return view;

        return new StudentAttendanceReportView
        {
            Id = id,
            StudentId = studentId,
            InstitutionId = institutionId,
            AcademicPeriodId = academicPeriodId,
            Year = year,
            Month = month,
            AbsentDays = []
        };
    }

    private static Guid GenerateDeterministicId(Guid studentId, int year, int month)
    {
        var bytes = studentId.ToByteArray();
        bytes[0] ^= (byte)(year & 0xFF);
        bytes[1] ^= (byte)((year >> 8) & 0xFF);
        bytes[2] ^= (byte)(month & 0xFF);
        return new Guid(bytes);
    }
}
