using Marten;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Policies;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Attendance.Shared.Events;
using Wolverine;

namespace MESNET.Attendance.Application.Handlers;

/// <summary>
/// Kademeli devamsızlık bildirimi — <b>5., 15. ve 25.</b> gün eşiklerini ölçer ve gerekiyorsa
/// tebligat gereğini yayınlar (#247, md. 36 (4)).
///
/// <para><b>Fesih zincirinden AYRIDIR.</b> O ayrı bir eşikten (md. 36 (5)) doğar
/// (<see cref="CheckAttendanceLimitHandler"/>) ve hüküm üretir; bu handler yalnız <b>tebligat
/// gereği</b> üretir. İkisi aynı sayaçtan beslenir ama farklı sonuç doğurur.</para>
///
/// <para><b>Üç giriş noktası</b> — sayılabilir küme her değiştiğinde: kayıt girildiğinde,
/// onaylandığında ve düzeltildiğinde. Gerekçe <see cref="CheckAttendanceLimitHandler"/> ile
/// aynı: yalnız girişi dinlemek, onay bekleyen kaydın dışlanmasıyla birleşince bildirimi de
/// sessizce öldürür.</para>
///
/// <para><b>İki ayak ayrı değerlendirilir</b> ve aynı çağrıda ikisi birden bildirim üretebilir.
/// Bu yüzden dönüş <c>OutgoingMessages</c>'tır: Wolverine koleksiyondaki her mesajı ayrı
/// cascading mesaj olarak yayınlar.</para>
///
/// <para><b>Defter aynı transaction'da yazılır.</b> <c>IDocumentSession</c> alındığı için
/// Wolverine Marten middleware'i uygular; kademe ilerlemesi ile yayınlanan mesaj aynı
/// transaction'da commit olur. Ayrı yazılsaydı mesaj gidip defter yazılmayabilir ve aynı kademe
/// tekrar tekrar bildirilirdi.</para>
/// </summary>
public static class CheckAbsenceNotificationHandler
{
    public static Task<OutgoingMessages> Handle(AttendanceMarked @event, IDocumentSession session)
        => EvaluateAsync(session, @event.StudentId, @event.AcademicPeriodId,
            @event.InstitutionId, @event.BusinessId);

    public static Task<OutgoingMessages> Handle(AttendanceApproved @event, IDocumentSession session)
        => EvaluateForRecordAsync(session, @event.AttendanceId);

    public static Task<OutgoingMessages> Handle(AttendanceCorrected @event, IDocumentSession session)
        => EvaluateForRecordAsync(session, @event.AttendanceId);

    /// <summary>
    /// Olay yalnız <c>AttendanceId</c> taşıyor; sayacın anahtarı aggregate'ten okunur — gerekçe
    /// <see cref="CheckAttendanceLimitHandler"/> ile aynı (saklı olayların şekli değiştirilemez,
    /// <c>Inline</c> snapshot her zaman günceldir).
    /// </summary>
    private static async Task<OutgoingMessages> EvaluateForRecordAsync(
        IDocumentSession session, Guid attendanceId)
    {
        var record = await session.LoadAsync<AttendanceRecord>(attendanceId);
        if (record is null) return [];

        return await EvaluateAsync(session, record.StudentId, record.AcademicPeriodId,
            record.InstitutionId, record.BusinessId);
    }

    private static async Task<OutgoingMessages> EvaluateAsync(
        IDocumentSession session, Guid studentId, Guid academicPeriodId,
        Guid institutionId, Guid businessId)
    {
        // Sayaç kapsamı öğrenci + akademik dönem (#242); sorgu bilerek geniş, süzme bellekte
        // (#252) — silinmiş ve onay bekleyen kayıtlar elenir.
        var records = await session.Query<AttendanceRecord>()
            .Where(r => r.StudentId == studentId && r.AcademicPeriodId == academicPeriodId)
            .ToListAsync();

        var tally = AttendanceCounterScope.Tally(records);

        var logId = AttendanceCounterScope.KeyFor(studentId, academicPeriodId);
        var log = await session.LoadAsync<AbsenceNotificationLog>(logId)
                  ?? new AbsenceNotificationLog
                  {
                      Id = logId,
                      InstitutionId = institutionId,
                      StudentId = studentId,
                      AcademicPeriodId = academicPeriodId
                  };

        var student = await session.LoadAsync<StudentNameView>(studentId);
        var now = DateTime.UtcNow;

        // Doğum tarihi bilinmiyorsa öğrenciye DE gönderilir — gönderme yönü güvenlidir.
        var notifyStudent = AbsenceNotificationPolicy.ShouldNotifyStudent(student?.BirthDate, now);

        var messages = new OutgoingMessages();

        // İki ayak BAĞIMSIZ: aynı çağrıda ikisi birden dolabilir.
        Consider(AbsenceNotificationLeg.Unexcused, tally.UnexcusedDays);
        Consider(AbsenceNotificationLeg.Total, tally.TotalDays);

        if (messages.Count > 0) session.Store(log);

        return messages;

        void Consider(string leg, int days)
        {
            var decision = AbsenceNotificationPolicy.Evaluate(leg, days, log.StepFor(leg));
            if (decision is null) return;

            log.Advance(leg, decision.Step, now);

            messages.Add(new AbsenceNotificationDue(
                studentId, businessId, institutionId, academicPeriodId,
                decision.Leg, decision.Step, decision.Days, decision.SkippedSteps,
                notifyStudent, now));
        }
    }
}
