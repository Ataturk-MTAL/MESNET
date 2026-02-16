namespace MESNET.Attendance.Core.Entities;

public class AttendanceView
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid BusinessId { get; set; }
    public int TotalAbsenceDays { get; set; }
    public int ExcusedDays { get; set; }
    public int UnexcusedDays { get; set; }
    public bool LimitExceeded { get; set; }
    public DateTime LastUpdated { get; set; }
}
