using MESNET.Attendance.Core.ValueObjects;

namespace MESNET.Attendance.Core.Entities;

public class WorkCalendar
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public int Year { get; set; }
    public List<CalendarDay> RestrictedDays { get; set; } = [];
    public string UpdatedBy { get; set; } = default!;
    public DateTime UpdatedAt { get; set; }
}
