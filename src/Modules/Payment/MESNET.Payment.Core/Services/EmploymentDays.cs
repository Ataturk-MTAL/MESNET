namespace MESNET.Payment.Core.Services;

/// <summary>
/// Bir sözleşmenin belirli bir ayda kaç gün istihdam ürettiğini sayar (#154). Saf fonksiyon.
/// </summary>
/// <remarks>
/// <b>Kural (01.08.2026 kararı): SGK usulü 30 günlük ay.</b> Ay tam çalışıldıysa ayın gün
/// sayısına bakılmaksızın 30 gün sayılır; eksik çalışıldıysa fiilî gün sayılır. Günlük ücret
/// <c>Taban / 30</c> olarak kalır (<c>business-rules.md</c> §6.2).
///
/// <para>Neden takvim böleni (<c>gün / ayın gün sayısı</c>) DEĞİL: o seçenek §6.2'nin sabit
/// bölenini kısmi ayda geçersiz kılar ve günlük ücreti aydan aya oynatır (Şubat'ta 357,14 TL,
/// Temmuz'da 322,58 TL). Dahası tam ay çalışan öğrenci Şubat'ta 28/30 = %93 ücret alırdı.</para>
///
/// <para><b>Bilinen ve kabul edilen sonuç:</b> 31 günlük ayda bölüşme olduğunda iki işverenin
/// gün toplamı 31 olur ve ödenen toplam tabanı aşar (31/30). Kırpma bilinçli olarak
/// eklenmedi — kırpmanın hangi işletmeden düşeceği keyfî bir karar gerektirirdi. Her işveren
/// kendi istihdam günü için sabit günlük ücreti öder.</para>
/// </remarks>
public static class EmploymentDays
{
    /// <summary>
    /// Tam ay gün sayısı. Hem oranlamanın böleni hem de günlük ücretin böleni budur —
    /// iki yerde ayrı sabit tutulmaz.
    /// </summary>
    public const int FullMonthDays = 30;

    /// <param name="start">Sözleşmenin başlangıç tarihi.</param>
    /// <param name="end">Bitiş tarihi; <c>null</c> = hâlâ yürürlükte.</param>
    /// <param name="year">Hesaplanan yıl.</param>
    /// <param name="month">Hesaplanan ay (1–12).</param>
    /// <returns>
    /// Ayın tamamı kapsanıyorsa <see cref="FullMonthDays"/>; kısmen kapsanıyorsa iki uç dahil
    /// fiilî gün sayısı; hiç kesişmiyorsa 0.
    /// </returns>
    public static int InMonth(DateTime start, DateTime? end, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = new DateTime(
            year, month, DateTime.DaysInMonth(year, month), 0, 0, 0, DateTimeKind.Utc);

        // Saat bileşeni atılır: fesih olayı EndDate'i gün ortasında taşıyabilir, gün bütün sayılır.
        var contractStart = start.Date;
        var contractEnd = (end ?? DateTime.MaxValue).Date;

        // Bozuk veri (bitiş < başlangıç) negatif gün üretmemeli.
        if (contractEnd < contractStart) return 0;

        var effectiveStart = contractStart > monthStart ? contractStart : monthStart;
        var effectiveEnd = contractEnd < monthEnd ? contractEnd : monthEnd;

        if (effectiveStart > effectiveEnd) return 0;

        if (effectiveStart == monthStart && effectiveEnd == monthEnd) return FullMonthDays;

        return (effectiveEnd - effectiveStart).Days + 1;
    }
}
