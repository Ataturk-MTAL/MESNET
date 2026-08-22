namespace MESNET.Payment.Core.Services;

/// <summary>
/// Bir fesih olayının hangi yerleştirmeyi kapattığına karar verir (#152).
///
/// <para>Saf fonksiyon: Marten oturumu ya da olay tipi gerektirmez, böylece kararın kendisi
/// altyapısız birim testlenebilir. Karar mantığı tüketicinin içinde kalsaydı, tam da bu
/// hatanın (fesihte yerleştirmenin kapanmaması) tekrar etmediğini doğrulamanın ucuz bir
/// yolu olmazdı.</para>
/// </summary>
public static class PlacementClosurePolicy
{
    /// <summary>
    /// Fesih bu yerleştirmeyi kapatmalı mı?
    ///
    /// <para><b>Neden yerleştirme kimliğiyle değil:</b> <c>ContractTerminated</c> yalnız
    /// <c>StudentId</c> ve <c>BusinessId</c> taşır, <c>PlacementId</c> taşımaz. Eşleşme bu
    /// ikiliyle yapılmak zorunda.</para>
    ///
    /// <para><b>Neden tarih kontrolü şart:</b> iş kuralı gereği öğrenci işletmeden işletmeye
    /// doğrudan geçemez — fesih, sonra yeni sözleşme. Aynı işletmeyle yeniden sözleşme
    /// yapılırsa (nadir ama mümkün) öğrenci+işletme ikilisi ESKİ ve YENİ yerleştirmenin
    /// ikisini birden bulur. Feshin ANINDAN SONRA başlamış yerleştirme, o feshin konusu
    /// olamaz; kapatılırsa öğrenci fiilen çalışırken maaşı kesilirdi.</para>
    /// </summary>
    /// <param name="placementStudentId">Yerleştirmedeki öğrenci.</param>
    /// <param name="placementBusinessId">Yerleştirmedeki işletme.</param>
    /// <param name="placementIsActive">Yerleştirme hâlâ açık mı.</param>
    /// <param name="placedAt">Yerleştirmenin başlangıç anı.</param>
    /// <param name="terminatedStudentId">Feshedilen sözleşmenin öğrencisi.</param>
    /// <param name="terminatedBusinessId">Feshedilen sözleşmenin işletmesi.</param>
    /// <param name="terminatedAt">Fesih anı.</param>
    public static bool ShouldClose(
        Guid placementStudentId,
        Guid placementBusinessId,
        bool placementIsActive,
        DateTime placedAt,
        Guid terminatedStudentId,
        Guid terminatedBusinessId,
        DateTime terminatedAt)
    {
        if (!placementIsActive) return false;
        if (placementStudentId != terminatedStudentId) return false;
        if (placementBusinessId != terminatedBusinessId) return false;

        // Fesihten SONRA başlamış yerleştirme bu feshin konusu değildir.
        return placedAt <= terminatedAt;
    }
}
