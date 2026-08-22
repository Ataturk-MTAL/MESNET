namespace MESNET.Reporting.Core.ReadModels;

/// <summary>
/// Öğrenci-ay bazlı devamsızlık kayıtları — Attendance modülü event'lerinden oluşturulur.
/// Her öğrenci × yıl × ay kombinasyonu için tek bir kayıt tutulur.
/// </summary>
public class StudentAttendanceReportView
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid InstitutionId { get; set; }
    public Guid AcademicPeriodId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>
    /// Devamsız günler listesi (gün numarası + devamsızlık türü)
    /// </summary>
    public List<AbsentDayEntry> AbsentDays { get; set; } = [];
}

/// <summary>
/// Tek bir devamsızlık gün kaydı.
/// </summary>
/// <param name="AttendanceId">
/// Kaynak devamsızlık kaydının kimliği (#257). <b>Sona eklendi ve varsayılanı boş:</b> bu alandan
/// önce yazılmış satırlar bozulmadan deserialize olur.
///
/// <para><b>Neden gerekli:</b> <c>AttendanceCorrected</c> / <c>AttendanceDeleted</c> /
/// <c>AttendanceApproved</c> olayları <b>tarih taşımıyor</b>, yalnız <c>AttendanceId</c>. Kimlik
/// satırda tutulmadığı için bu tüketiciler hangi günü güncelleyeceklerini bilemiyor ve
/// <b>boş no-op</b> olarak duruyorlardı: yanlış girilip düzeltilen ya da silinen devamsızlık
/// resmî formda kalıcı hâle geliyordu.</para>
/// </param>
/// <param name="StatusName">
/// <c>Pending</c> / <c>Recorded</c> / <c>Verified</c> / <c>Corrected</c> (#257).
///
/// <para><b>Onay bekleyen kayıt resmî forma yazılmaz</b> — işletmenin tek taraflı bildirimi,
/// koordinatör öğretmen onaylamadan velinin ve idarenin gördüğü belgede devamsızlık olarak
/// görünemez (#172, #252 ile aynı ilke).</para>
///
/// <para><b>Bilinmeyen durum GÖSTERİLİR.</b> Bu alandan önce yazılmış satırlarda değer yoktur;
/// onları gizlemek var olan formlardan veri silmek olurdu. Onarım
/// <c>POST /api/attendance/resync-snapshots</c> (#256) ile yapılır — dağıtım ön koşuludur.</para>
/// </param>
public sealed record AbsentDayEntry(
    int Day,
    string AbsenceType,
    Guid AttendanceId = default,
    string? StatusName = null)
{
    /// <summary>Bu kayıt resmî forma yazılır mı — onay bekleyen yazılmaz (#257).</summary>
    public bool IsOfficial =>
        !string.Equals(StatusName, "Pending", StringComparison.Ordinal);
}
