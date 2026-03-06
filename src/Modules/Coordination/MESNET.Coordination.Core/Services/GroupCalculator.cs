namespace MESNET.Coordination.Core.Services;

/// <summary>
/// Norm Kadro Yönetmeliği Madde 22 — öğrenci sayısına göre grup hesaplama.
/// Örgün program (Madde 22/1-ç) ve MESEM program (Madde 22/2) ayrı tablolar.
/// </summary>
public static class GroupCalculator
{
    /// <summary>
    /// Örgün program grup hesabı (Madde 22/1-ç).
    /// 9. sınıf ve 10-12. sınıf için farklı eşikler.
    /// Maksimum 4 grup (kaynaştırma istisnası ile 5).
    /// </summary>
    public static int CalculateFormalGroups(int classYear, int studentCount)
    {
        if (studentCount <= 0) return 0;

        return classYear switch
        {
            9 => studentCount switch
            {
                < 10 => 0,
                < 21 => 1,
                < 31 => 2,
                _ => 3
            },
            >= 10 and <= 12 => studentCount switch
            {
                < 8 => 0,
                < 17 => 1,
                < 25 => 2,
                < 33 => 3,
                _ => 4
            },
            _ => 0
        };
    }

    /// <summary>
    /// MESEM program (Mesleki Eğitim Merkezi) grup hesabı (Madde 22/2).
    /// Çırak/kursiyer sayısına göre grup. Şubeler gruplara bölünmez.
    /// </summary>
    public static int CalculateMesemGroups(int apprenticeCount)
    {
        if (apprenticeCount <= 0) return 0;

        return apprenticeCount switch
        {
            < 10 => 0,
            < 41 => 1,
            < 81 => 2,
            < 121 => 3,
            < 161 => 4,
            < 201 => 5,
            < 241 => 6,
            < 281 => 7,
            < 321 => 8,
            < 361 => 9,
            < 401 => 10,
            < 441 => 11,
            _ => 12
        };
    }

    /// <summary>
    /// Eğitim tipine göre uygun grup hesaplama metodunu çağırır.
    /// </summary>
    public static int CalculateGroups(string educationType, int classYear, int studentCount) =>
        educationType == "Mesem"
            ? CalculateMesemGroups(studentCount)
            : CalculateFormalGroups(classYear, studentCount);
}
