namespace MESNET.Coordination.Core.Services;

/// <summary>
/// Saat dağıtım algoritmasının işletme başına girdisi.
/// Saf veri taşıyıcı — hiçbir read-model'e bağımlı değildir, çağıran katman kendi
/// projeksiyonundan bu kaydı doldurur.
/// </summary>
/// <param name="BusinessId">İşletme kimliği.</param>
/// <param name="BranchCode">Planlamanın yapıldığı alan kodu (planlama alan bazlıdır).</param>
/// <param name="MaxHours">
/// Mesafeye göre üst sınır (<c>max_i</c>) — <c>CoordinationCalculator.CalculateMaxHours</c> çıktısı.
/// 1'in altındaki değer o işletmeye ücretli saat verilemeyeceği anlamına gelir.
/// </param>
/// <param name="StudentCount">Alanın o işletmedeki aktif öğrenci sayısı (<c>s_i</c>).</param>
/// <param name="IsPinned">Koordinatör bu satırı elle kilitledi mi.</param>
/// <param name="PinnedHours">Kilitli satırın saati — algoritma bu değeri asla değiştirmez.</param>
public sealed record AllocationInput(
    Guid BusinessId,
    string BranchCode,
    int MaxHours,
    int StudentCount,
    bool IsPinned,
    int PinnedHours);
