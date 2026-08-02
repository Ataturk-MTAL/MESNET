namespace MESNET.Common.Shared.Security;

/// <summary>
/// Veli kapsamı kuralları (#174). Saf fonksiyonlar — G/Ç yapmaz.
///
/// <para><b>Veli kapsamı permission ile verilemez</b> (ADR-0001: izin erişimi açar, kapsamı
/// belirlemez). "Hangi öğrencinin verisi" sorusunun cevabı bir izin değil bir <b>kayıt</b>tır:
/// <c>UserAccount.LinkedStudentIds</c>. Desen <c>BranchCodes</c> (#126) ile birebir aynıdır —
/// kayıt otoriterdir, token'dan gelen değer ezilir.</para>
///
/// <para><b>Neden izin yetmez:</b> velinin izinleri (<c>attendance:upload</c>,
/// <c>attendance:leave:request</c>) tüm velilerde aynıdır; onları birbirinden ayıran tek şey
/// hangi öğrenciye bağlı oldukları. İzin bazlı bir çözüm ya her veliyi her öğrenciye açardı ya
/// da öğrenci başına izin üretmeyi gerektirirdi.</para>
/// </summary>
public static class ParentScopePolicy
{
    /// <summary>
    /// Bağ kaydı olan bir kullanıcı bu öğrencinin verisine erişebilir mi.
    ///
    /// <para><b>Boş liste erişim vermez.</b> <c>BranchCodes</c>'ta boş liste "hiçbir alana
    /// bağlı değil" demekti ve muafiyeti olan roller için normaldi; burada boş liste
    /// <b>bağ kurulmamış</b> demektir ve hiçbir öğrenciye erişim doğurmaz.</para>
    /// </summary>
    public static bool CanAccessStudent(IReadOnlyList<Guid>? linkedStudentIds, Guid studentId) =>
        studentId != Guid.Empty
        && linkedStudentIds is { Count: > 0 }
        && linkedStudentIds.Contains(studentId);

    /// <summary>
    /// Kullanıcı bir öğrenciye bağlı mı — yani kapsamı bağ kaydından mı okunacak.
    ///
    /// <para>Bu kontrol <b>rol adına bakmaz</b>: "veli mi" sorusu değil "bağlı öğrencisi var mı"
    /// sorusu sorulur. Rol yeniden adlandırılsa da kayıt yerinde kalır (ADR-0001).</para>
    /// </summary>
    public static bool HasLinkedStudents(IReadOnlyList<Guid>? linkedStudentIds) =>
        linkedStudentIds is { Count: > 0 };

    /// <summary>
    /// Bağ listesini normalleştirir: boş kimlikler atılır, yinelenenler teklenir.
    /// Kayıt yazılmadan önce tek yerden geçsin diye burada.
    /// </summary>
    public static List<Guid> Normalize(IEnumerable<Guid>? studentIds) =>
        (studentIds ?? [])
        .Where(id => id != Guid.Empty)
        .Distinct()
        .ToList();
}
