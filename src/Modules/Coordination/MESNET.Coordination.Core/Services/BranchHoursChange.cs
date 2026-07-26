using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Core.Services;

/// <summary>
/// Toplu kayıtta <b>tek bir satırın</b> yeni hâli (#117).
/// Marten'den yüklenmiş satır ile komuttan gelen istek burada birleşir; politika
/// yalnız bu düz veriyi görür, oturuma dokunmaz.
/// </summary>
/// <param name="BusinessId">İşletme kimliği — hata mesajında "hangi işletme" bilgisi.</param>
/// <param name="BusinessName">İşletme adı — kullanıcı kimliği değil adı okur.</param>
/// <param name="RequestedHours">Kullanıcının girdiği saat. Fahri satırda yok sayılır.</param>
/// <param name="IsHonoraryVisit">Fahri (ücretsiz) ziyaret işareti (#115).</param>
/// <param name="MaxCoordinationHours">Satırın mesafe tavanı (<c>max_i</c>).</param>
/// <param name="AssignedTeacherId">Atanmış öğretmen — yoksa öğretmen kısıtı işlemez.</param>
/// <param name="AssignedTeacherName">Öğretmen adı — hata mesajı için.</param>
public sealed record BranchHoursChange(
    Guid BusinessId,
    string BusinessName,
    int RequestedHours,
    bool IsHonoraryVisit,
    int MaxCoordinationHours,
    Guid? AssignedTeacherId = null,
    string? AssignedTeacherName = null)
{
    /// <summary>
    /// Ücret doğuran saat. Fahri satırda her zaman 0 — kullanıcı fahri işaretlerken
    /// girdide eski saat kalmış olabilir, komutu reddetmek yerine sıfırlıyoruz (#115).
    /// </summary>
    public int EffectiveBillableHours() => IsHonoraryVisit ? 0 : RequestedHours;

    /// <summary>
    /// Öğretmen kapasitesine sayılan hedef saat. Ücretli satırda saat zaten
    /// <c>&gt; 0</c> doğrulandığı için mesafe tavanına düşme dalı devreye girmez.
    /// </summary>
    public int EffectiveBillableTargetHours() =>
        IsHonoraryVisit ? 0 : (RequestedHours > 0 ? RequestedHours : MaxCoordinationHours);
}

/// <summary>
/// Politikanın gördüğü tüm girdi (#117). Değişmeyen satırların katkısı önceden
/// toplanıp buraya taşınır — böylece politika saf kalır ve birim testlenebilir.
/// </summary>
/// <param name="Changes">Kaydedilmek istenen satırlar.</param>
/// <param name="OtherBillableHours">
/// Aynı alan+dönemde <b>değişmeyen</b> satırların ücretli saat toplamı. Havuz kısıtı
/// tüm sete birden uygulanır; issue #117'nin kökü buydu: tekil çağrılarda değişen
/// satırların eski değerleri de toplama giriyor, sonuç çağrı sırasına bağlı oluyordu.
/// </param>
/// <param name="TotalWorkloadPool">Ders yükü havuzu. <c>null</c> → havuz yapılandırılmamış, kısıt uygulanmaz.</param>
/// <param name="OtherTeacherBillableHours">
/// Öğretmen → o öğretmenin <b>değişmeyen</b> satırlarından gelen hedef saat toplamı.
/// </param>
/// <param name="MaxWeeklyExtraHours">Öğretmen başına azami haftalık ek ders saati. <c>null</c> → kısıt uygulanmaz.</param>
public sealed record BranchHoursValidationInput(
    IReadOnlyList<BranchHoursChange> Changes,
    int OtherBillableHours,
    int? TotalWorkloadPool,
    IReadOnlyDictionary<Guid, int> OtherTeacherBillableHours,
    int? MaxWeeklyExtraHours);

/// <summary>
/// Kırılan tek kısıt (#117). Hangi kısıtın hangi işletmede kırıldığını taşır —
/// kullanıcı "bir yerde hata var" değil, düzeltmesi gereken satırı görür.
/// </summary>
public sealed record BranchHoursViolation(
    HoursViolationKind Kind,
    int Attempted,
    int Limit,
    Guid? BusinessId = null,
    string? BusinessName = null,
    Guid? TeacherId = null,
    string? TeacherName = null,
    IReadOnlyList<string>? AffectedBusinessNames = null)
{
    /// <summary>Toplu kaydın atomikliğini her mesajda tekrarlayan son cümle.</summary>
    private const string NothingSaved = " Toplu kayıt iptal edildi, hiçbir satır yazılmadı.";

    /// <summary>
    /// Kullanıcıya gösterilecek Türkçe açıklama. Mesaj Core'da üretilir ki
    /// birim testler kullanıcının gördüğü metnin ta kendisini doğrulayabilsin.
    /// </summary>
    public string Describe()
    {
        var business = string.IsNullOrWhiteSpace(BusinessName) ? "—" : BusinessName;
        var affected = AffectedBusinessNames is { Count: > 0 }
            ? string.Join(", ", AffectedBusinessNames)
            : business;

        if (Kind == HoursViolationKind.InvalidAssignedHours)
        {
            return $"«{business}» için takdir edilen saat 0'dan büyük olmalıdır: {Attempted}." + NothingSaved;
        }

        if (Kind == HoursViolationKind.AssignedHoursExceedMax)
        {
            return $"«{business}» için takdir edilen saat ({Attempted}) verilebilir saati ({Limit}) aşamaz."
                   + NothingSaved;
        }

        if (Kind == HoursViolationKind.WorkloadPoolExceeded)
        {
            return $"Toplam takdir edilen saat ({Attempted}) ders yükü havuzunu ({Limit}) aşamaz. "
                   + $"Kaydedilmek istenen işletmeler: {affected}." + NothingSaved;
        }

        var teacher = string.IsNullOrWhiteSpace(TeacherName) ? TeacherId?.ToString() ?? "—" : TeacherName;
        return $"«{teacher}» öğretmeninin toplam koordinatörlük saati ({Attempted}) azami limiti ({Limit}) aşıyor. "
               + $"Etkilenen işletmeler: {affected}." + NothingSaved;
    }
}
