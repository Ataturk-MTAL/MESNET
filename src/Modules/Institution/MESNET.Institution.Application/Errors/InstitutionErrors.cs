using MESNET.Common.Shared;
using MESNET.Common.Shared.Reference;

namespace MESNET.Institution.Application.Errors;

public static class InstitutionErrors
{
    /// <summary>
    /// Aktör başka bir okulun verisine dokunmaya çalıştı (ADR-0003 adım 6).
    ///
    /// <para>Hedef kurum kimliği <b>mesajda geçtiği hâliyle</b> yazılır; aktörünki yazılmaz —
    /// hata mesajı, kapsamı olmayan bir kullanıcıya başka okulun varlığını doğrulatmamalıdır.</para>
    /// </summary>
    public static Error InstitutionScopeDenied(Guid targetInstitutionId) =>
        new("Institution.ScopeDenied",
            $"Bu işlem yalnız kendi kurumunuz için yapılabilir (hedef: {targetInstitutionId}).");

    /// <summary>
    /// İlçe, kurumun iline ait değil. İl ve ilçe ayrı ayrı güncellenebildiği için nihai
    /// kombinasyon handler'da doğrulanır (#147).
    /// </summary>
    public static Error DistrictNotInProvince(string districtName, string? provinceCode) =>
        new("Institution.DistrictNotInProvince",
            $"\"{districtName}\" ilçesi kurumun iline ({TurkishProvinces.GetName(provinceCode) ?? provinceCode ?? "tanımsız"}) ait değil.");

    /// <summary>Aynı kişi ikinci kez yetkilendirilemez (#190).</summary>
    public static Error StaffAlreadyAuthorized(string keycloakId) =>
        new("Institution.StaffAlreadyAuthorized",
            $"Bu kullanıcı kurumda zaten yetkilendirilmiş: {keycloakId}. " +
            "Rol ya da alan değişikliği için mevcut kaydı güncelleyin.");

    /// <summary>
    /// Palet anahtarı küratörlü kümede yok. Serbest renk kabul edilmediği için bu, girdi
    /// hatasıdır: geçerli anahtarlar mesajda sayılır ki çağıran katalog ucuna gitmeden
    /// düzeltebilsin.
    /// </summary>
    public static Error UnknownBrandPalette(string paletteName, IEnumerable<string> allowed) =>
        new("Institution.UnknownBrandPalette",
            $"Tanınmayan marka paleti: \"{paletteName}\". Geçerli seçenekler: {string.Join(", ", allowed)}.");

    public static Error NotFound(Guid id) =>
        new("Institution.NotFound", $"Kurum bulunamadı: {id}");

    public static Error BranchAlreadyActive(string fieldCode) =>
        new("Institution.BranchAlreadyActive", $"Alan '{fieldCode}' zaten aktif.");

    public static Error FieldOfStudyNotFound(string fieldCode) =>
        new("Institution.FieldOfStudyNotFound", $"Eğitim alanı bulunamadı: {fieldCode}");

    public static Error BranchNotFound(string fieldCode) =>
        new("Institution.BranchNotFound", $"Aktif alan bulunamadı: {fieldCode}");

    public static Error InvalidPeriodCount(int count, int min, int max) =>
        new("Institution.InvalidPeriodCount",
            $"Günlük ders sayısı {min}-{max} arasında olmalıdır. Girilen: {count}");

    public static Error AcademicPeriodAlreadyExists(string name) =>
        new("Institution.AcademicPeriodAlreadyExists", $"Bu dönem zaten mevcut: {name}");

    public static Error AcademicPeriodNotFound(Guid id) =>
        new("Institution.AcademicPeriodNotFound", $"Dönem bulunamadı: {id}");

    public static Error AcademicPeriodAlreadyClosed(Guid id) =>
        new("Institution.AcademicPeriodAlreadyClosed", $"Dönem zaten kapatılmış: {id}");

    public static Error NoActiveAcademicPeriod(Guid institutionId) =>
        new("Institution.NoActiveAcademicPeriod", $"Kurumun aktif dönemi bulunmuyor: {institutionId}");

    public static Error InvalidSupervisorCount() =>
        new("Institution.InvalidSupervisorCount", "Şef sayısı negatif olamaz.");

    public static Error InvalidGradeEntryWindow() =>
        new("Institution.InvalidGradeEntryWindow", "Not giriş penceresi bitiş tarihi, başlangıç tarihinden önce olamaz.");
}
