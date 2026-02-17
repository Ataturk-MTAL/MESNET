using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

public sealed class ExamResult : SmartEnum<ExamResult>
{
    public static readonly ExamResult Passed = new(nameof(Passed), 1, "Başarılı");
    public static readonly ExamResult Failed = new(nameof(Failed), 2, "Başarısız");

    private ExamResult(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public string Slug { get; }
}
