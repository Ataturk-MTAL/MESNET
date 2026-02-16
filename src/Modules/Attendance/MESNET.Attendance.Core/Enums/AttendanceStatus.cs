using Ardalis.SmartEnum;

namespace MESNET.Attendance.Core.Enums;

public sealed class AttendanceStatus : SmartEnum<AttendanceStatus>
{
    public static readonly AttendanceStatus Recorded = new(nameof(Recorded), 1, "Kaydedildi");
    public static readonly AttendanceStatus Verified = new(nameof(Verified), 2, "Doğrulandı");
    public static readonly AttendanceStatus Corrected = new(nameof(Corrected), 3, "Düzeltildi");

    public string Slug { get; }

    private AttendanceStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    private static readonly Dictionary<AttendanceStatus, HashSet<AttendanceStatus>> Transitions = new()
    {
        [Recorded] = [Verified, Corrected],
        [Verified] = [Corrected],
        [Corrected] = [Verified]
    };

    public bool CanTransitionTo(AttendanceStatus target)
        => Transitions.TryGetValue(this, out var allowed) && allowed.Contains(target);
}
