namespace MESNET.Common.Shared.Security;

/// <summary>
/// Personel kaydından kullanıcı hesabına <b>hangi boşluğun</b> doldurulacağına karar verir
/// (ADR-0003 adım 2.1).
///
/// <para><b>Neden gerekli:</b> kiracı anahtarının otoritesi <c>UserAccount.InstitutionId</c>'dir,
/// Keycloak değil. Ama mevcut kullanıcıların çoğunda o alan <b>boş</b> — kurum bilgisi bugüne
/// kadar token claim'inden okunuyordu. Backfill yapılmadan token yolu kapatılırsa mevcut
/// kullanıcılar <b>kapsamsız kalır ve kilitlenir</b>; ADR bu yüzden backfill'i ayrı deploy
/// olarak sıralıyor.</para>
///
/// <para><b>İki kural, ikisi de "uydurma yok, üzerine yazma yok":</b></para>
/// <list type="bullet">
///   <item><b>Yalnız boşluk doldurulur.</b> İdarenin elle girdiği değer, personel kaydından
///   gelen tahminle ezilmez.</item>
///   <item><b>Boş kaynak veri yazılmaz.</b> Branşsız personel (müdür, müdür yardımcısı) normal
///   bir durumdur, eksik veri değil.</item>
/// </list>
///
/// <para><b>Kritik ayrım:</b> branşın boş olması kurum backfill'ini <b>engellemez</b>. Aynı
/// olayda iki ayrı boşluk vardır ve biri diğerinin ön koşulu değildir — tek bir erken dönüş,
/// okul müdürünün kiracı anahtarını sessizce doldurulmamış bırakırdı.</para>
/// </summary>
public static class StaffAccountBackfillPolicy
{
    /// <summary>
    /// Kurum (kiracı) anahtarı doldurulmalı mı.
    /// </summary>
    /// <param name="staffInstitutionId">Personel kaydındaki kurum.</param>
    /// <param name="accountInstitutionId">Kullanıcı kaydındaki kurum; <c>null</c> = boşluk.</param>
    public static bool ShouldFillInstitution(Guid staffInstitutionId, Guid? accountInstitutionId)
    {
        if (staffInstitutionId == Guid.Empty) return false;

        return accountInstitutionId is null || accountInstitutionId == Guid.Empty;
    }

    /// <summary>
    /// Alan (branş) kapsamı doldurulmalı mı.
    /// </summary>
    /// <param name="staffBranchCode">
    /// Personelin alanı; boş olabilir ve bu <b>geçerlidir</b> — müdür ve müdür yardımcısı
    /// hiçbir alana bağlı değildir (#126).
    /// </param>
    /// <param name="accountBranchCodes">Kullanıcı kaydındaki alanlar; doluysa dokunulmaz.</param>
    public static bool ShouldFillBranches(
        string? staffBranchCode, IReadOnlyCollection<string> accountBranchCodes)
    {
        if (string.IsNullOrWhiteSpace(staffBranchCode)) return false;

        return accountBranchCodes.Count == 0;
    }
}
