namespace MESNET.Payment.Core.Services;

/// <summary>
/// Bir devamsızlık günü <b>ücret kesintisine sayılır mı</b> — tek karar noktası (#255).
///
/// <para><b>Neden tek yerde:</b> aynı yüklem iki yerde gerekiyor. <c>PaymentSaga</c> kesintiyi
/// <i>hesaplarken</i> sorar; <c>AbsenceTallyConsumer</c> ise devamsızlık kaydı değiştiğinde
/// <i>yeniden hesap gerekiyor mu</i> diye sorar. İkisi ayrışırsa sonuç sessizdir: tetikleyici
/// "değişmedi" der, hesap hiç koşmaz ve tutar eski kalır — ne hata ne log.</para>
///
/// <para><b>İki eksen birden:</b></para>
/// <list type="bullet">
///   <item><b>Tür</b> — yalnız <c>Unexcused</c> ve <c>UnpaidLeave</c> ücreti keser
///   (business-rules.md §6.2). Sağlık raporu, mazeretli devamsızlık ve ücretli izin kesmez.</item>
///   <item><b>Durum</b> — <c>Pending</c> kayıt sayılmaz (#172, #252). İşletmenin tek taraflı
///   girişi koordinatör öğretmen onaylamadan ücreti kesemez; ödemeyi yapan taraf kendi
///   kesintisini koyamaz.</item>
/// </list>
/// </summary>
public static class AbsenceDeductionPolicy
{
    /// <summary>
    /// Ücret kesintisine tabi devamsızlık türleri — <c>AbsenceType.AffectsSalary</c> ile aynı küme.
    ///
    /// <para>Düz string: SmartEnum Marten LINQ'inde kullanılamaz (bkz. CLAUDE.md). Dizi
    /// <c>PaymentSaga</c>'nın LINQ sorgusunda <c>Contains</c> ile doğrudan kullanılır.</para>
    /// </summary>
    public static readonly string[] DeductibleAbsenceTypes = ["Unexcused", "UnpaidLeave"];

    /// <summary>Onay bekleyen kayıt — <c>AttendanceStatus.Pending</c> adı.</summary>
    public const string PendingStatus = "Pending";

    /// <summary>
    /// Bu tür + durum ikilisi ücret kesintisine <b>sayılır mı</b>.
    ///
    /// <para><b>Bilinmeyen tür SAYILMAZ</b> — kesinti öğrenci aleyhine bir hükümdür ve tanınmayan
    /// veriden doğamaz. Bu, devamsızlık <i>sınırının</i> yönünün tersidir (orada bilinmeyen durum
    /// sayılır, çünkü orada eksik veri sınırı gevşetmemeli); iki yönün farklı olması bilinçlidir:
    /// biri parayı keser, diğeri mevzuat eşiğini korur.</para>
    /// </summary>
    public static bool CountsTowardDeduction(string? absenceTypeName, string? statusName)
        => absenceTypeName is not null
           && DeductibleAbsenceTypes.Contains(absenceTypeName, StringComparer.Ordinal)
           && !string.Equals(statusName, PendingStatus, StringComparison.Ordinal);
}
