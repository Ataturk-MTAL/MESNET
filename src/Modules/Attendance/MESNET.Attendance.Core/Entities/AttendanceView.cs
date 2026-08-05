namespace MESNET.Attendance.Core.Entities;

public class AttendanceView
{
    public Guid Id { get; set; }

    /// <summary>
    /// Kiracı anahtarı (#147). Türetilmiş görünüm olsa da kiracıya ait veri taşır; anahtarsız
    /// hâlinde çok-okul sorgusu iki okulun satırını ayırt edemezdi.
    /// </summary>
    public Guid InstitutionId { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public int TotalAbsenceDays { get; set; }
    public int ExcusedDays { get; set; }
    public int UnexcusedDays { get; set; }
    public bool LimitExceeded { get; set; }
    public DateTime LastUpdated { get; set; }
}
