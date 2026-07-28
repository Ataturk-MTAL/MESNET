namespace MESNET.Attendance.Application.Commands;

/// <param name="MarkedById">
/// Kaydı giren kullanıcının kimliği (#139). <b>Ad taşınmaz</b>, tüketici çözer.
///
/// <para>Bu mesaj Wolverine durable local queue'ya konur; tüketilene kadar
/// <c>wolverine</c> şemasında bekler. Ad burada taşınsaydı, kuyrukta beklerken kullanıcı
/// adını değiştirdiğinde bildirim eski adı gösterirdi. Kimlik taşıyıp adı tüketim anında
/// <c>UserNameView</c>'dan çözmek bu pencereyi kapatır.</para>
/// </param>
public sealed record NotifyAttendancePendingApproval(
    Guid AttendanceId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid CoordinatorTeacherId,
    Guid MarkedById,
    DateTime Date,
    string AbsenceType);
