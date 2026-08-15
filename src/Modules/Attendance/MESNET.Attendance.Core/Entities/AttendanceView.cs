namespace MESNET.Attendance.Core.Entities;

public class AttendanceView
{
    /// <summary>
    /// <c>{studentId}:{academicPeriodId}</c> — <see cref="Policies.AttendanceCounterScope.KeyFor"/>.
    /// Okunabilir tutuldu: operatör satırı veritabanında gözle bulabilmeli (#242).
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// Kiracı anahtarı (#147). Türetilmiş görünüm olsa da kiracıya ait veri taşır; anahtarsız
    /// hâlinde çok-okul sorgusu iki okulun satırını ayırt edemezdi.
    /// </summary>
    public Guid InstitutionId { get; set; }
    public Guid StudentId { get; set; }

    /// <summary>Sayaç dönem başında sıfırlanır — anahtarın parçası (#242).</summary>
    public Guid AcademicPeriodId { get; set; }
    /// <summary>
    /// <b>Yalnız bilgi.</b> Sayacın kapsamına GİRMEZ — devamsızlık öğrencinin eğitim yılına
    /// aittir, işletmeye değil (#242). En son devamsızlığın girildiği işletmeyi tutar.
    /// </summary>
    public Guid BusinessId { get; set; }
    public int TotalAbsenceDays { get; set; }
    public int ExcusedDays { get; set; }
    public int UnexcusedDays { get; set; }
    public bool LimitExceeded { get; set; }
    public DateTime LastUpdated { get; set; }
}
