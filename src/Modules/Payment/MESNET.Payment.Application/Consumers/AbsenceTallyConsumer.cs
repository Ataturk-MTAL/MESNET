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
