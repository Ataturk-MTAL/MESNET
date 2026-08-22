using Ardalis.SmartEnum;

namespace MESNET.Attendance.Core.Enums;

public sealed class AbsenceType : SmartEnum<AbsenceType>
{
    public static readonly AbsenceType Excused = new(nameof(Excused), 1, "Mazeretli");
    public static readonly AbsenceType Unexcused = new(nameof(Unexcused), 2, "Mazeretsiz");
    public static readonly AbsenceType HealthReport = new(nameof(HealthReport), 3, "Sağlık Raporu");

    // MEB Ortaöğretim Kurumları Yönetmeliği, işletmenin yükümlülükleri:
    //   (j) "bir ders yılı içinde devamsızlıktan sayılmak ve en çok devamsızlık süresini
    //        geçmemek üzere ... ÜCRETSİZ mazeret izni verir"
    //   (ı) "telafi eğitimi süresince ve okulda yapılacak sınavlar için ... ÜCRETLİ izin verir"
    //   (i) "ara tatil, yarıyıl ve yaz tatili süresince toplam bir ay ÜCRETLİ izin verir"
    // Bu iki kategori yoktu; ücretsiz izin ancak "Mazeretli" olarak girilebiliyordu ve
    // AffectsSalary=false olduğu için ücret kesilmiyordu (#83).
    public static readonly AbsenceType UnpaidLeave = new(nameof(UnpaidLeave), 4, "Ücretsiz İzin");
    public static readonly AbsenceType PaidLeave = new(nameof(PaidLeave), 5, "Ücretli İzin");

    public string Slug { get; }

    private AbsenceType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    /// <summary>
    /// Ücret kesintisine tabi mi. business-rules.md §6.2: "Özürsüz devamsızlık ve ücretsiz izin
    /// günlerinde ücret kesilir." Ücretli izin, mazeretli devamsızlık ve sağlık raporunda
    /// kesinti yapılmaz.
    /// </summary>
    public bool AffectsSalary => this == Unexcused || this == UnpaidLeave;
}
