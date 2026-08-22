namespace MESNET.Attendance.Shared.Events;

/// <param name="MarkedById">
/// Kaydı giren kullanıcının kimliği — token'ın <c>sub</c> claim'inden gelir (#139).
/// Ad SAKLANMAZ; okuma anında <c>UserNameView</c>'dan çözülür.
///
/// <para><b>Neden yeni ad, eski <c>MarkedBy</c> alanının tipini değiştirmek yerine:</b>
/// bu olay <c>shared.mt_events</c> içinde kalıcıdır ve <c>AttendanceRecord</c> her okumada
/// ondan replay edilir. Aynı adı <c>Guid</c> yapmak, saklı <c>"markedBy": "Ahmet Yılmaz"</c>
/// değerini okunamaz kılar ve replay'i <c>JsonException</c> ile kırardı — yalnız denetim adı
/// değil, devamsızlık kaydının kendisi okunamaz hâle gelirdi. Yeni ad eski anahtarı sessizce
/// yok sayar; eski adlar kaybolur — bilinçli kabul edilen kayıp.</para>
/// </param>
public sealed record AttendanceMarked(
    Guid AttendanceId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime Date,
    string AbsenceType,
    Guid MarkedById,
    string InitialStatus);
