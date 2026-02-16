using Ardalis.SmartEnum;

namespace MESNET.Attendance.Core.Enums;

public sealed class CalendarDayType : SmartEnum<CalendarDayType>
{
    public static readonly CalendarDayType PublicHoliday = new(nameof(PublicHoliday), 1, "Resmi Tatil");
    public static readonly CalendarDayType Weekend = new(nameof(Weekend), 2, "Hafta Sonu");
    public static readonly CalendarDayType SchoolLeave = new(nameof(SchoolLeave), 3, "Okul İzni");
    public static readonly CalendarDayType SemesterBreak = new(nameof(SemesterBreak), 4, "Sömestr Tatili");
    public static readonly CalendarDayType MidBreak = new(nameof(MidBreak), 5, "Ara Tatil");
    public static readonly CalendarDayType EmergencyLeave = new(nameof(EmergencyLeave), 6, "Olağanüstü Tatil");

    public string Slug { get; }

    private CalendarDayType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
