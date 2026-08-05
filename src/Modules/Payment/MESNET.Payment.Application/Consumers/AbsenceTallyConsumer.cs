using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Attendance olaylarından Payment'ın yerel devamsızlık kaydını besler.
/// Maaş kesintisi bu kayıtlar sayılarak hesaplanır (#64).
/// </summary>
/// <remarks>
/// <c>AttendanceVerified</c> / <c>AttendanceApproved</c> / <c>AttendanceCorrected</c> /
/// <c>AttendanceDeleted</c> olayları tarih ve işletme bilgisi taşımıyor, yalnız
/// <c>AttendanceId</c> var — bu yüzden kayıt <c>AttendanceMarked</c>'ta kurulup sonraki
/// olaylarda kimlikten yüklenerek güncelleniyor.
/// </remarks>
public static class AbsenceTallyConsumer
{
    public static void Consume(AttendanceMarked @event, IDocumentSession session)
    {
        session.Store(new StudentAbsenceView
        {
            Id = @event.AttendanceId,
            InstitutionId = @event.InstitutionId,
            StudentId = @event.StudentId,
            BusinessId = @event.BusinessId,
            Month = @event.Date.ToString("yyyy-MM"),
            // Gün, kesintinin hangi sözleşmeye yazılacağını belirler (#154).
            Date = @event.Date.Date,
            AbsenceTypeName = @event.AbsenceType,
            StatusName = @event.InitialStatus
        });
    }

    // İşletmenin girdiği kayıt öğretmence onaylandı: Pending → Recorded.
    public static async Task Consume(AttendanceApproved @event, IDocumentSession session)
        => await SetStatus(session, @event.AttendanceId, "Recorded");

    public static async Task Consume(AttendanceVerified @event, IDocumentSession session)
        => await SetStatus(session, @event.AttendanceId, "Verified");

    public static async Task Consume(AttendanceCorrected @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<StudentAbsenceView>(@event.AttendanceId);
        if (view is null) return;

        view.AbsenceTypeName = @event.NewAbsenceType;
        view.StatusName = "Corrected";
        session.Store(view);
    }

    /// <summary>
    /// Sağlık raporu ONAYLANDI (#172) — devamsızlık türü <c>HealthReport</c>'a döner ve o tür
    /// ücret kesintisine tabi değildir (business-rules.md §6.2). Kesinti bu anda kalkar.
    /// </summary>
    public static async Task Consume(HealthReportApproved @event, IDocumentSession session)
        => await SetHealthReportType(session, @event.AttendanceId);

    /// <summary>
    /// Rapor yüklendi (#172). Kesinti YALNIZ okul tarafının doğrudan girdiği raporda kalkar
    /// (<c>RequiresApproval = false</c>) — onay zaten o rollerde biter.
    ///
    /// <para>İşletme, usta öğretici, işletme İK ya da öğrenci yüklediğinde
    /// <c>RequiresApproval = true</c>'dur ve burada HİÇBİR ŞEY yapılmaz: ödemeyi yapan taraf
    /// kendi kesintisini tek taraflı kaldıramaz. Tür ancak koordinatör öğretmenin onayıyla,
    /// <c>HealthReportApproved</c> üzerinden değişir.</para>
    ///
    /// <para>#172 öncesinde bu olay HİÇ dinlenmiyordu; Attendance modülünde tür değişse bile
    /// Payment'ın yerel kaydı eski türde kalıyor ve geçerli raporu olan öğrencinin ücreti
    /// kesilmeye devam ediyordu. Aynı düzeltme o boşluğu da kapatır.</para>
    /// </summary>
    public static async Task Consume(HealthReportAttached @event, IDocumentSession session)
    {
        if (@event.RequiresApproval) return;

        await SetHealthReportType(session, @event.AttendanceId);
    }

    private static async Task SetHealthReportType(IDocumentSession session, Guid attendanceId)
    {
        var view = await session.LoadAsync<StudentAbsenceView>(attendanceId);
        if (view is null) return;

        view.AbsenceTypeName = "HealthReport";
        session.Store(view);
    }

    public static void Consume(AttendanceDeleted @event, IDocumentSession session)
        => session.Delete<StudentAbsenceView>(@event.AttendanceId);

    private static async Task SetStatus(IDocumentSession session, Guid attendanceId, string status)
    {
        var view = await session.LoadAsync<StudentAbsenceView>(attendanceId);
        if (view is null) return;

        view.StatusName = status;
        session.Store(view);
    }
}
