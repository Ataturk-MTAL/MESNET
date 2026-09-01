using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Core.Policies;

/// <summary>
/// Bir (şube, öğretim türü) için sınıf bazlı aktif öğrenci sayısı.
/// <see cref="Counts"/> <b>boş olabilir</b> ve bu bir eksiklik değildir — o şubede aktif
/// öğrenci kalmadığı anlamına gelir.
/// </summary>
public sealed record BranchStudentCount(
    string BranchCode, string EducationTypeName, Dictionary<int, int> Counts);

/// <summary>
/// Şube öğrenci sayacının <b>mutlak</b> hesabı (#290).
///
/// <para><b>Neden saf bir sınıf:</b> bu sayı doğrudan para kararına giriyor —
/// <c>BranchStudentCountView</c> → <c>UpsertBranchWorkloadConfig</c> → <c>GroupCalculator</c>
/// (Norm Kadro Yön. Md.22) → <c>BranchWorkloadConfig.TotalWorkloadPool</c>, ve o havuz
/// <c>AssignBusinessToTeacher</c>'ın <b>sert tavanıdır</b>. Kararın Marten'sız sınanabilir
/// olması gerekiyordu.</para>
/// </summary>
public static class StudentCountPolicy
{
    /// <summary>
    /// <b>Gruplama TÜM öğrenciler üzerinden, sayım yalnız aktifler üzerinden.</b>
    ///
    /// <para>Sıra tersine çevrilirse (önce süz, sonra grupla) aktif öğrencisi <b>sıfıra düşmüş</b>
    /// bir şube hiç grup üretmez, dolayısıyla o şube için hiç olay yayınlanmaz ve tüketicideki
    /// satır <b>eski değerinde donar</b>. Ölçüldü: kusur tüketicide değil, yayıncının
    /// süzgecindeydi.</para>
    ///
    /// <para>Boş sözlük bilerek üretilir: "dokunma" ile "sıfırla" farklı şeylerdir. Tüketici
    /// sözlüğü replace ettiği için boş sözlük sayacı sıfırlar.</para>
    /// </summary>
    public static IReadOnlyList<BranchStudentCount> ActiveCountsByBranch(
        IEnumerable<StudentProfile> students) =>
        students
            .GroupBy(s => new { s.BranchCode, s.EducationTypeName })
            .Select(g => new BranchStudentCount(
                g.Key.BranchCode,
                g.Key.EducationTypeName,
                g.Where(s => !s.Status.IsFinal)
                    .GroupBy(s => s.ClassYear)
                    .ToDictionary(cg => cg.Key, cg => cg.Count())))
            .ToList();
}
