using MESNET.Enrollment.Core.Enums;

namespace MESNET.Enrollment.Core.Policies;

/// <summary>
/// Yerleştirme türü ile işletme kimliğinin tutarlılığı (#159). Saf fonksiyon — G/Ç yapmaz.
/// </summary>
/// <remarks>
/// İki yön de hatadır ve ikisi de sessizce geçmemelidir:
/// <list type="bullet">
/// <item>İşletmede staj, işletmesiz olamaz — ücret kime yazılacağı belirsiz kalır.</item>
/// <item>Okulda staj, işletmeli olamaz — işletme varsa sözleşme kurulabilir ve sistem kanuna
/// aykırı biçimde ücret + katkı hesaplar.</item>
/// </list>
/// </remarks>
public static class PlacementTypePolicy
{
    public static bool IsConsistent(PlacementType type, Guid? businessId)
        => type.RequiresBusiness ? businessId.HasValue : !businessId.HasValue;

    /// <summary>
    /// Okulda staj mı — ücret, devlet katkısı ve dekont yükümlülüğünün doğmadığı hâl.
    /// Karar <b>işletmenin yokluğundan</b> okunur: diğer modüller türü bilmeden de doğru
    /// davranabilsin diye tek ölçüt <c>BusinessId</c>'dir.
    /// </summary>
    public static bool IsSchoolBased(Guid? businessId) => !businessId.HasValue;
}
