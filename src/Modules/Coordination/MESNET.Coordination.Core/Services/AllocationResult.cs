using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Core.Services;

/// <summary>
/// İşletme başına saat önerisi. <see cref="Weight"/> alanı "neden bu işletmeye N saat"
/// sorusunun tek satırlık cevabıdır.
/// </summary>
/// <param name="Weight">w = tavan saat × öğrenci sayısı.</param>
/// <param name="SuggestedHours">Önerilen saat; fahri satırlarda 0.</param>
/// <param name="IsHonoraryVisit">Havuz yetmediği için ücretsiz (fahri) ziyarete düşen satır.</param>
public sealed record AllocationLine(
    Guid BusinessId,
    string BranchCode,
    int MaxHours,
    int StudentCount,
    long Weight,
    int SuggestedHours,
    bool IsPinned,
    bool IsHonoraryVisit,
    AllocationBucket Bucket);

/// <summary>
/// Öneriyle birlikte dönen tanılama. Koordinatörün "havuz nereye gitti" sorusunu
/// ekranda cevaplar; hiçbir artık sessizce yutulmaz.
/// </summary>
/// <param name="Pool">Ders yükü havuzu (<c>P</c>).</param>
/// <param name="TeacherCapacity">Alan öğretmeni boş saat kapasitesi (<c>C</c>).</param>
/// <param name="SumOfMax">Σ max_i — tüm işletmeler tavanına çıksa gereken saat.</param>
/// <param name="TotalAllocated">Σ önerilen saat (sabitlenmiş satırlar dahil).</param>
/// <param name="Undistributed">
/// <c>P − TotalAllocated</c>. Pozitif değer dağıtılamayan havuz artığıdır.
/// Negatif değer yalnız sabitlenmiş saatler havuzu aştığında oluşur ve gizlenmez.
/// </param>
/// <param name="HonoraryCount">Fahri kovasındaki işletme sayısı.</param>
/// <param name="OutOfBranchHours">Alan dışına önerilen toplam saat.</param>
/// <param name="IsPoolUndefined">Havuz hesaplanmamış (<c>P ≤ 0</c>) — öneri üretilmedi.</param>
public sealed record AllocationDiagnostics(
    int Pool,
    int TeacherCapacity,
    int SumOfMax,
    int TotalAllocated,
    int Undistributed,
    int HonoraryCount,
    int OutOfBranchHours,
    bool IsPoolUndefined);

/// <summary>
/// Saat dağıtım algoritmasının çıktısı: satır önerileri + tanılama.
/// Satırlar her zaman öncelik sırasındadır (ağırlık ↓, tavan ↓, alan kodu ↑, kimlik ↑).
/// </summary>
public sealed record AllocationResult(
    IReadOnlyList<AllocationLine> Lines,
    AllocationDiagnostics Diagnostics);
