using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

public sealed class EvaluationResult : SmartEnum<EvaluationResult>
{
    public static readonly EvaluationResult Suitable = new(nameof(Suitable), 1, "Uygun");
    public static readonly EvaluationResult Unsuitable = new(nameof(Unsuitable), 2, "Uygun Değil");
    public static readonly EvaluationResult Conditional = new(nameof(Conditional), 3, "Şartlı Uygun");

    private EvaluationResult(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public string Slug { get; }
}
