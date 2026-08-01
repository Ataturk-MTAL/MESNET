using Ardalis.SmartEnum;

namespace MESNET.Payment.Core.Enums;

public sealed class GovernmentContributionType : SmartEnum<GovernmentContributionType>
{
    public static readonly GovernmentContributionType MemStudent = new(nameof(MemStudent), 1, "MEB Öğrencisi");
    public static readonly GovernmentContributionType NonMemLarge = new(nameof(NonMemLarge), 2, "MEM Dışı - Büyük");
    public static readonly GovernmentContributionType NonMemSmall = new(nameof(NonMemSmall), 3, "MEM Dışı - Küçük");
    public static readonly GovernmentContributionType PublicInstitution = new(nameof(PublicInstitution), 4, "Kamu Kurumu");

    /// <summary>
    /// Sınıf tekrarı (#161): öğrenci bu sınıf yılı için katkıyı zaten almış. Ücret ödenmeye
    /// devam eder, katkı sıfırdır — işveren payı (Net − Katkı) o ay yükselir. Ayrı bir değer
    /// olması şart: işletme "neden bu ay katkı gelmedi" sorusunu kayıttan cevaplayabilmeli.
    /// </summary>
    public static readonly GovernmentContributionType ClassYearRepeated = new(nameof(ClassYearRepeated), 5, "Sınıf Tekrarı");

    public string Slug { get; }

    private GovernmentContributionType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
