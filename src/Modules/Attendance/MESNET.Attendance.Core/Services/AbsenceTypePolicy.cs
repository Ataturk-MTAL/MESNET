using MESNET.Attendance.Core.Enums;

namespace MESNET.Attendance.Core.Services;

/// <summary>
/// Devamsızlık türü giriş kuralları (#175). Saf fonksiyonlar — G/Ç yapmaz.
///
/// <para>İki ayrı kural vardır ve karıştırılmamalıdır: <b>kim</b> hangi türü bildirebilir
/// (<see cref="CanReport"/>) ve bir tür <b>hangi eğitim türünde</b> geçerlidir
/// (<see cref="IsValidForEducationType"/>).</para>
/// </summary>
public static class AbsenceTypePolicy
{
    /// <summary>Mesleki eğitim merkezi (MESEM) öğrencisi — <c>StudentRegistered.EducationType</c>.</summary>
    public const string MesemEducationType = "Mesem";

    /// <summary>
    /// İşletme tarafı bu türü bildirebilir mi (#175).
    ///
    /// <para>Sahibin kuralı: <i>"İşletme resmî izin veremez, devamsızlığı bildirir. Veli iznine
    /// çevrilebiliyorsa çevrilir, veli dilekçesiyle — o öğrenci işlerinin işi."</i> Yani işletme
    /// "öğrenci gelmedi" der; <b>sınıflandırma okulundur</b>. Mazeretli, ücretli/ücretsiz izin ve
    /// sağlık raporu birer sınıflandırma kararıdır ve her biri ücreti etkiler.</para>
    ///
    /// <para>Karar rol adına değil <c>attendance:direct-entry</c> iznine bakar (ADR-0001).
    /// Okul tarafı türü <c>/correct</c> ile değiştirir; sağlık raporu ise kendi onay zincirinden
    /// geçer (#172) — işletme raporu yükler, öğretmen onaylar, tür o anda değişir.</para>
    /// </summary>
    public static bool CanReport(AbsenceType type, bool hasDirectEntry) =>
        hasDirectEntry || type == AbsenceType.Unexcused;

    /// <summary>
    /// Tür bu eğitim türünde geçerli mi (#175).
    ///
    /// <para>Sahibin kuralı: <i>"MESEM'lerde ücretli izin var ama örgün eğitimde ücretli izin
    /// hakkı yok — rapor ya da veli izni şart."</i> Ücretli izin kesinti doğurmaz; örgün
    /// öğrencide kullanılırsa öğrenci hak etmediği bir gün için tam ücret alır.</para>
    ///
    /// <para><b>Eğitim türü bilinmiyorsa REDDEDİLİR.</b> İzin verilseydi eksik veri sessizce
    /// para sonucu doğururdu; reddetmek görünür bir hata üretir ve
    /// <c>POST /api/enrollment/students/resync-projections</c> ile düzelir.</para>
    ///
    /// <para><b>Bu kontrol ÖN KOŞULDUR ve başvuru anında çalışır (#177).</b> "Hangi öğrencide
    /// mümkün" sorusunu cevaplar; "resmileşti mi" sorusunu <see cref="RequiresApprovedRequest"/>
    /// cevaplar. Örgün öğrenci ücretli izin başvurusu bile açamaz.</para>
    /// </summary>
    public static bool IsValidForEducationType(AbsenceType type, string? educationTypeName) =>
        type != AbsenceType.PaidLeave || IsMesem(educationTypeName);

    /// <summary>
    /// Bu tür yalnız resmîleşen bir başvurudan doğabilir mi (#177).
    ///
    /// <para>Ücretli izin artık <b>doğrudan girilemez</b> — okul tarafı bile giremez. Öğrenci
    /// başvurur, işletme onaylar, okul onaylar; kayıtlar ancak o zaman
    /// <c>PaidLeaveAttendanceConsumer</c> tarafından açılır.</para>
    ///
    /// <para><b>Neden okul tarafına da kapalı:</b> ücretli izin kesinti doğurmaz, yani türü
    /// seçmek doğrudan para kararıdır. Doğrudan giriş açık kalsaydı iki taraflı onay zinciri
    /// tek komutla atlanabilirdi — tıpkı #172 öncesinde <c>/correct</c> ucunun sağlık raporu
    /// zincirini atlaması gibi. Kısıt komut yolundadır; onaydan doğan kayıtlar olay tüketicisiyle
    /// açıldığı için bu kapıdan geçmez.</para>
    /// </summary>
    public static bool RequiresApprovedRequest(AbsenceType type) => type == AbsenceType.PaidLeave;

    private static bool IsMesem(string? educationTypeName) =>
        string.Equals(educationTypeName, MesemEducationType, StringComparison.OrdinalIgnoreCase);
}
