using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

/// <summary>
/// Dönem notu durum akışı: işletme girer (Draft) → gönderir (Submitted) → okul fişi üretirken
/// okul-payı alanlarını tamamlayıp kesinleştirir (Finalized).
/// </summary>
public sealed class StudentTermGradeStatus : SmartEnum<StudentTermGradeStatus>
{
    public static readonly StudentTermGradeStatus Draft = new(nameof(Draft), 1, "Taslak");
    public static readonly StudentTermGradeStatus Submitted = new(nameof(Submitted), 2, "Gönderildi");
    public static readonly StudentTermGradeStatus Finalized = new(nameof(Finalized), 3, "Kesinleşti");

    public string Slug { get; }

    private StudentTermGradeStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
