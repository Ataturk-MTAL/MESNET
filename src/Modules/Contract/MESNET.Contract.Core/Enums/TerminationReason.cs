using Ardalis.SmartEnum;

namespace MESNET.Contract.Core.Enums;

public sealed class TerminationReason : SmartEnum<TerminationReason>
{
    public static readonly TerminationReason Discipline = new(nameof(Discipline), 1, "Disiplin");
    public static readonly TerminationReason Health = new(nameof(Health), 2, "Sağlık");
    public static readonly TerminationReason AttendanceLimitExceeded = new(nameof(AttendanceLimitExceeded), 3, "Devamsızlık Aşımı");
    public static readonly TerminationReason BusinessRequest = new(nameof(BusinessRequest), 4, "İşletme Fesih Talebi");
    public static readonly TerminationReason StudentRequest = new(nameof(StudentRequest), 5, "Öğrenci Talebi");
    public static readonly TerminationReason Other = new(nameof(Other), 6, "Diğer");

    public string Slug { get; }

    private TerminationReason(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
