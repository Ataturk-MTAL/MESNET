using MESNET.Attendance.Core.ValueObjects;

namespace MESNET.Attendance.Core.Entities;

public class WorkCalendar
{
    public Guid Id { get; set; }
    public Guid InstitutionId { get; set; }
    public int Year { get; set; }
    public List<CalendarDay> RestrictedDays { get; set; } = [];
    /// <summary>
    /// Son değişikliği yapan kullanıcının kimliği — token'dan gelir, istekten ALINMAZ (#137).
    /// Ad sorgu tarafında <c>UserNameView</c>'dan çözülür. Eski <c>updatedBy</c> JSON
    /// anahtarı (serbest metin ad) bu adla artık okunmaz.
    /// </summary>
    public Guid UpdatedById { get; set; }
    public DateTime UpdatedAt { get; set; }
}
