namespace MESNET.Attendance.Shared.Events;

/// <summary>
/// Bir devamsızlık kaydının <b>o anki tam hâli</b> — diğer modüllerin yerel görünümlerini
/// geçmişe dönük onarmak için (#256).
///
/// <para><b>Neden ayrı bir olay, <c>AttendanceMarked</c>'ı yeniden yayınlamak yerine:</b>
/// <c>AttendanceMarked</c> devamsızlık <b>sınırını</b> yeniden ölçtürür
/// (<c>CheckAttendanceLimitHandler</c>) ve sınır dolmuşsa fesih onay zincirini yeniden
/// başlatırdı. Onarım amaçlı bir yeniden yayının hüküm doğurması kabul edilemez; bu olayın
/// tüketicisi yalnız görünüm besleyen taraftır.</para>
///
/// <para><b>Neden tam durum taşır:</b> <c>AttendanceApproved</c> / <c>AttendanceCorrected</c>
/// gibi artımlı olaylar tarih ve tür taşımıyor; onarım için kaydın <b>bugünkü</b> hâli gerekir.
/// Kaynak olay akışı değil <c>AttendanceRecord</c> agregasıdır — tür, durum ve
/// <see cref="IsDeleted"/> orada günceldir.</para>
/// </summary>
/// <param name="Status">
/// <c>Pending</c> / <c>Recorded</c> / <c>Verified</c> / <c>Corrected</c>. Onay bekleyen kayıt
/// ücret kesintisine sayılmaz (#172, #252) — alan bu yüzden taşınmak zorunda.
/// </param>
/// <param name="IsDeleted">
/// Silinmiş kayıt: tüketici yerel satırı <b>silmelidir</b>. Onarımın en çok işe yaradığı hâl
/// budur — silinmiş devamsızlıktan ücret kesilmeye devam etmiş olabilir.
/// </param>
public sealed record AttendanceSnapshotResynced(
    Guid AttendanceId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime Date,
    string AbsenceType,
    string Status,
    bool IsDeleted);
