using Marten;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.Services;
using MESNET.Attendance.Shared.Events;
using Wolverine;

namespace MESNET.Attendance.Application.Consumers;

/// <summary>
/// Resmîleşen ücretli izinden devamsızlık kayıtlarını açar (#177).
///
/// <para><b>Neden komut değil tüketici:</b> ücretli izin komut yolundan artık girilemez
/// (<see cref="AbsenceTypePolicy.RequiresApprovedRequest"/>) ve <c>MarkAttendance</c>'ın "yalnız
/// bu hafta" kısıtı ileri tarihli izni reddederdi. Kayıtlar zincirin sonundaki olaydan doğar;
/// böylece hüküm tek bir yerden, onaylanmış başvurudan gelir.</para>
///
/// <para><b>Yeniden çalıştırmaya dayanıklıdır:</b> Wolverine mesajı yeniden işlerse aynı gün için
/// ikinci kayıt açılmaz — o gün zaten <c>PaidLeave</c> ise atlanır.</para>
///
/// <para><b>Üretilen olaylar ayrıca YAYINLANIR.</b> Bir tüketici içinde <c>StartStream</c>/
/// <c>Append</c> ile yazılan olaylar Marten akışına girer ama mesaj olarak kendiliğinden
/// yayılmaz; yayılmasaydı Payment ve Reporting'in yerel devamsızlık kayıtları bu günlerden
/// habersiz kalırdı (<c>AbsenceTallyConsumer</c> kaydı <c>AttendanceMarked</c>'ta kurar).</para>
/// </summary>
public static class PaidLeaveAttendanceConsumer
{
    /// <summary>Onaydan doğan kayıtlarda düzeltme aktörü — serbest metin alan (#139 öncesi şema).</summary>
    private const string SystemActor = "Ücretli izin onayı (sistem)";

    public static async Task Consume(
        PaidLeaveApproved @event, IDocumentSession session, IMessageBus bus)
    {
        var restrictedDays = await LoadRestrictedDaysAsync(session, @event);

        var leaveDays = PaidLeaveApprovalPolicy.ExpandLeaveDays(
            @event.StartDate, @event.EndDate, restrictedDays);

        if (leaveDays.Count == 0) return;

        var existingByDay = await LoadExistingRecordsAsync(session, @event);

        foreach (var day in leaveDays)
        {
            if (existingByDay.TryGetValue(day, out var existing))
            {
                // O gün için zaten kayıt var. Türü ücretli izne çevirmezsek onaylanmış izne
                // rağmen ücret kesintisi sürerdi — sessiz para hatası. Zaten ücretli izinse
                // dokunulmaz (yeniden işleme dayanıklılık).
                if (existing.Type == AbsenceType.PaidLeave) continue;

                var corrected = new AttendanceCorrected(
                    existing.Id, existing.StudentId, SystemActor,
                    AbsenceType.PaidLeave.Name, @event.Reason, DateTime.UtcNow);

                session.Events.Append(existing.Id, corrected);
                await bus.PublishAsync(corrected);
                continue;
            }

            var attendanceId = Guid.NewGuid();
            var marked = new AttendanceMarked(
                attendanceId,
                @event.StudentId,
                @event.BusinessId,
                @event.InstitutionId,
                @event.AcademicPeriodId,
                day,
                AbsenceType.PaidLeave.Name,
                @event.ApprovedById,
                // İzin okul onayından geçti; kayıt ayrıca onaya düşmez.
                AttendanceStatus.Recorded.Name);

            session.Events.StartStream<AttendanceRecord>(attendanceId, marked);
            await bus.PublishAsync(marked);
        }
    }

    /// <summary>
    /// Kurum takvimindeki kısıtlı günler. Aralık yıl sınırını aşabildiği için takvim yıl başına
    /// tutulur ve aralığın kapsadığı her yıl yüklenir.
    /// </summary>
    private static async Task<List<DateTime>> LoadRestrictedDaysAsync(
        IQuerySession session, PaidLeaveApproved @event)
    {
        var years = Enumerable
            .Range(@event.StartDate.Year, @event.EndDate.Year - @event.StartDate.Year + 1)
            .ToArray();

        var calendars = await session.Query<WorkCalendar>()
            .Where(c => c.InstitutionId == @event.InstitutionId && years.Contains(c.Year))
            .ToListAsync();

        return calendars.SelectMany(c => c.RestrictedDays.Select(d => d.Date)).ToList();
    }

    /// <summary>
    /// Aralıktaki mevcut devamsızlık kayıtları, gün başına. Tarih karşılaştırması bellekte
    /// yapılır: kayıtlarda saat bileşeni olabilir, LINQ'te gün eşitliği güvenilmez.
    /// </summary>
    private static async Task<Dictionary<DateTime, AttendanceRecord>> LoadExistingRecordsAsync(
        IQuerySession session, PaidLeaveApproved @event)
    {
        var rangeStart = @event.StartDate.Date;
        var rangeEnd = @event.EndDate.Date.AddDays(1);

        var records = await session.Query<AttendanceRecord>()
            .Where(r => r.StudentId == @event.StudentId
                && r.Date >= rangeStart
                && r.Date < rangeEnd
                && !r.IsDeleted)
            .ToListAsync();

        return records
            .GroupBy(r => r.Date.Date)
            .ToDictionary(g => g.Key, g => g.First());
    }
}
