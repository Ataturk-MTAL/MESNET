using Ardalis.SmartEnum;

namespace MESNET.Common.Shared.Enums;

/// <summary>
/// 3308 sayılı Kanun'un ayırdığı öğrenci kategorileri (#85).
///
/// Madde 25 ücret tabanını kategoriye göre farklılaştırıyor: "aday çırak ve çırağa
/// <b>yaşına uygun asgari ücretin yüzde otuzundan</b>", işletmelerde mesleki eğitim gören
/// öğrencilere ise işletme büyüklüğüne göre %15/%30 (MESEM 12. sınıf kalfalık yeterliğiyle %50).
/// </summary>
public sealed class StudentCategory : SmartEnum<StudentCategory>
{
    /// <summary>İşletmelerde mesleki eğitim gören / staj yapan öğrenci — varsayılan.</summary>
    public static readonly StudentCategory Student = new(nameof(Student), 1, "Öğrenci");

    public static readonly StudentCategory CandidateApprentice = new(nameof(CandidateApprentice), 2, "Aday Çırak");

    public static readonly StudentCategory Apprentice = new(nameof(Apprentice), 3, "Çırak");

    public string Slug { get; }

    /// <summary>Madde 25'teki "aday çırak ve çırak" tabanına (yaşına uygun asgari ücretin %30'u) tabi mi.</summary>
    public bool IsApprentice => this == CandidateApprentice || this == Apprentice;

    private StudentCategory(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
