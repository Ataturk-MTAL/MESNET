namespace MESNET.Common.Shared.Security;

/// <summary>
/// JWT claim'lerden oluşturulan kullanıcı bağlamı.
/// Tüm modüller tarafından ortaklaşa kullanılır.
/// </summary>
public sealed record UserContext(
    Guid UserId,
    string FullName,
    Guid? InstitutionId = null,
    Guid? BusinessId = null,
    Guid? StudentId = null,
    IReadOnlyList<string>? Roles = null,
    IReadOnlyList<string>? Permissions = null,
    /// <summary>
    /// Kullanıcının sorumlu olduğu alan (branş) kodları — <c>branch_codes</c> claim'i (#126).
    /// Bir alan şefi birden çok alandan sorumlu olabildiği için listedir.
    /// </summary>
    IReadOnlyList<string>? BranchCodes = null,
    /// <summary>
    /// Velinin bağlı olduğu öğrenciler — <c>linked_student_ids</c> claim'i (#174).
    /// Bir veli birden çok öğrenciye bağlı olabildiği için listedir. Boş liste "bağ kurulmamış"
    /// demektir ve hiçbir öğrenciye erişim doğurmaz.
    /// </summary>
    IReadOnlyList<Guid>? LinkedStudentIds = null,
    /// <summary>
    /// Aktörün kurum ağacındaki yolu — <c>institution_path</c> claim'i. Kapsam kararının
    /// ağaç aşamasında kullanılır: hedefin yolu bununla başlıyorsa erişim vardır.
    ///
    /// <para><c>null</c> = geçiş ucu bu kullanıcının kurumu için henüz koşmadı. O durumda
    /// kapsam kimlik eşitliğine düşer, yani bugünkü davranış korunur.</para>
    ///
    /// <para><b>Kaynağı kurum kaydıdır, token DEĞİL</b> — <c>institution_id</c> ile aynı
    /// disiplin (ADR-0003 adım 2). Kullanıcının yazabildiği bir yol, kullanıcının kendi
    /// kapsamını seçmesi demektir.</para>
    /// </summary>
    string? InstitutionPath = null,
    /// <summary>
    /// Aktörün adına davrandığı kurum — <c>active_institution_id</c> claim'i (B parçası).
    /// <c>null</c> = kendi kurumunda çalışıyor.
    /// </summary>
    /// <remarks>
    /// <b><see cref="InstitutionId"/> ile karıştırmayın.</b> O "kim olduğun", bu "nerede
    /// davrandığın". Denetim izi ikisini ayrı alanlara yazar ve
    /// <c>CrossedTenantBoundary</c> tam olarak bu farktan doğar.
    /// </remarks>
    Guid? ActiveInstitutionId = null);
