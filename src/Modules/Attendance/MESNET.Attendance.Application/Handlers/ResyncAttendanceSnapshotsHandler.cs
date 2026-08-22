using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Shared.Events;
using Wolverine;

namespace MESNET.Attendance.Application.Handlers;

/// <summary>
/// Devamsızlık kayıtlarının bugünkü hâlini yeniden yayınlar — diğer modüllerin yerel
/// görünümlerini geçmişe dönük onarmak için (#256).
///
/// <para><b>Neden gerekti:</b> <c>AttendanceApproved</c> / <c>AttendanceCorrected</c> /
/// <c>AttendanceVerified</c> / <c>AttendanceDeleted</c> / <c>HealthReportApproved</c> /
/// <c>HealthReportAttached</c> olayları uzun süre <b>hiç teslim edilmiyordu</b>:
/// <c>[AggregateHandler]</c> dönüşü Marten akışına yazılır ama mesaj olarak yayılmaz (#253).
/// Payment'ın <c>StudentAbsenceView</c> satırları bu yüzden <c>AttendanceMarked</c> anındaki
/// hâlde <b>donmuş</b> durumda: agregada <c>Recorded</c> olan kayıt görünümde <c>Pending</c>,
/// onaylanmış raporlu gün hâlâ <c>Unexcused</c>, silinen kayıt hiç silinmemiş olabilir.</para>
///
/// <para><b>Etkisi iki yönlüdür:</b> eksik kesinti (görünüm <c>Pending</c> donmuş) ya da
/// <b>fazla kesinti</b> (onaylanmış rapor işlenmemiş, silinen kayıt duruyor). İkincisi öğrenci
/// aleyhinedir.</para>
///
/// <para><b>Kaynak agregadır, olay akışı DEĞİL.</b> Akışı yeniden oynatmak
/// <c>AttendanceMarked</c>'ı da yayınlar ve o, devamsızlık sınırını yeniden ölçtürüp fesih onay
/// zincirini başlatır. Agregada tür, durum ve <c>IsDeleted</c> zaten günceldir.</para>
///
/// <para><b>Kiracı başına çalışır</b> — <c>AttendanceRecord</c> kiracıya ait bir belgedir ve bu
/// uç istek bağlamındadır; her okul için ayrı çağrılmalıdır. Yayınlanan mesajlar
/// <c>IMessageBus.TenantId</c>'yi devralır.</para>
///
/// <para><b>İdempotenttir:</b> tüketiciler upsert/delete yapar, tekrar çağrılabilir.</para>
/// </summary>
public static class ResyncAttendanceSnapshotsHandler
{
    public static async Task<ResyncAttendanceSnapshotsResult> Handle(
        ResyncAttendanceSnapshots command, IQuerySession session, IMessageBus bus,
        CancellationToken ct)
    {
        // Silinmiş kayıtlar DA yayılır: tüketicinin yerel satırı silebilmesi için tek yol bu.
        IQueryable<AttendanceRecord> query = session.Query<AttendanceRecord>();

        if (command.AcademicPeriodId is { } periodId)
            query = query.Where(r => r.AcademicPeriodId == periodId);

        var records = await query.ToListAsync(ct);

        foreach (var record in records)
        {
            await bus.PublishAsync(new AttendanceSnapshotResynced(
                record.Id,
                record.StudentId,
                record.BusinessId,
                record.InstitutionId,
                record.AcademicPeriodId,
                record.Date,
                // SmartEnum modüller arası olaylarda string taşınır (CLAUDE.md).
                record.Type?.Name ?? "",
                record.Status?.Name ?? "",
                record.IsDeleted));
        }

        return new ResyncAttendanceSnapshotsResult(
            records.Count, records.Count(r => r.IsDeleted));
    }
}
