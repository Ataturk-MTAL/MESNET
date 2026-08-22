namespace MESNET.Common.Shared.Security;

/// <summary>
/// Öğrenci kapsamının kullanıcı hesabına bağlanıp bağlanmayacağı kararı (#236).
/// </summary>
/// <param name="ShouldBind">Bağ kurulmalı mı.</param>
/// <param name="IsWarning">
/// Kararın <b>görünür</b> olması gerekiyor mu. "Yapılacak iş yok" ile "bilerek reddedildi"
/// aynı log seviyesinde kalırsa çakışma hiç fark edilmez.
/// </param>
/// <param name="Reason">Log satırına yazılacak gerekçe; hiçbir karar gerekçesiz kalmaz.</param>
public sealed record StudentAccountBindingDecision(bool ShouldBind, bool IsWarning, string Reason)
{
    public static readonly StudentAccountBindingDecision Bind =
        new(true, false, "Hesap boştu, öğrenci kapsamı bağlandı.");

    public static readonly StudentAccountBindingDecision AlreadyBound =
        new(false, false, "Hesap zaten aynı öğrenciye bağlı; yapılacak iş yok.");

    public static readonly StudentAccountBindingDecision ConflictingStudent =
        new(false, true, "Hesap BAŞKA bir öğrenciye bağlı; üzerine yazılmadı.");

    public static readonly StudentAccountBindingDecision InstitutionMismatch =
        new(false, true, "Hesabın kurumu olayın kurumuyla eşleşmiyor; bağlanmadı.");
}

/// <summary>
/// <c>UserAccount.StudentId</c> otoritesinin <b>hangi koşulda</b> yazılacağına karar verir (#236).
///
/// <para><b>Yaşanan açık:</b> tüketici yalnız "zaten aynı mı" diye bakıyordu; hesabın
/// <c>StudentId</c>'si <b>dolu ve farklı</b> olduğunda üzerine yazıyordu. Hesabın kurumu ile
/// olayın kurumu da hiç karşılaştırılmıyordu.</para>
///
/// <para><b>Neden başka hiçbir katman kapatmıyor:</b> <c>UserAccount</c> kimlik katmanı
/// belgesidir (<c>DocumentTenancyMap</c> → <c>Identity</c>) ve <b>kiracı damgası taşımaz</b>;
/// arama okullar arası globaldir. Conjoined kiracılık istek bağlamında süzer, bu kod olay
/// bağlamında çalışır. Kurum kapsam guard'ı da göremez — mesaj istekten kurum kimliği almaz.</para>
///
/// <para><b>Tetikleyici:</b> <c>RegisterStudent</c> komutu <c>KeycloakUserId</c>'yi doğrulamadan
/// alıyor (validator yalnız <c>NotEmpty</c>); yanlış bir kimlik başka okulun kullanıcısına
/// kapsam bağlar. Toplu içe aktarma (#238) bunu satır sayısıyla çarpar.</para>
///
/// <para><b>İki yönlü zarar</b> — üzerine yazmanın maliyeti tek taraflı değil: yeni sahip
/// başkasının verisini görür, <b>gerçek sahibin bağı kopar</b> ve o kişi kendi verisini görmeyi
/// bırakır. İkisi de hata vermez, boş sonuç verir.</para>
/// </summary>
public static class StudentAccountBindingPolicy
{
    /// <summary>
    /// Bağ kararını verir. <b>Sıra anlamlıdır</b> ve kararın parçasıdır.
    /// </summary>
    /// <param name="eventStudentId">Olaydaki öğrenci.</param>
    /// <param name="accountStudentId">Hesaptaki mevcut öğrenci; <c>null</c> = boşluk.</param>
    /// <param name="eventInstitutionId">Olayın kurumu.</param>
    /// <param name="accountInstitutionId">Hesabın kurumu; <c>null</c> = kapsamsız hesap.</param>
    public static StudentAccountBindingDecision Decide(
        Guid eventStudentId,
        Guid? accountStudentId,
        Guid eventInstitutionId,
        Guid? accountInstitutionId)
    {
        // 1. Aynı bağ — yapılacak iş yok. Kurum kontrolünden ÖNCE gelir: olay yeniden
        //    işlenebilir (Wolverine retry, resync ucu) ve her tekrarda uyarı üretmek gerçek
        //    uyarıları gürültüye gömerdi.
        if (accountStudentId == eventStudentId)
            return StudentAccountBindingDecision.AlreadyBound;

        // 2. Kurum çapraz kontrolü. Yalnız İKİSİ de doluyken uyuşmazlık ilan edilir:
        //    kapsamsız hesap "başka okula ait" kanıtı değildir ve katı davranmak meşru yolu
        //    kırardı (davetle açılan hesapta kurum henüz dolmamış olabilir). Kalan boşluk
        //    bilinçlidir — kapsamsız hesap hâlâ bağlanabilir.
        if (IsPresent(accountInstitutionId) && eventInstitutionId != Guid.Empty
            && accountInstitutionId != eventInstitutionId)
            return StudentAccountBindingDecision.InstitutionMismatch;

        // 3. Dolu ve farklı bağ — ÜZERİNE YAZILMAZ. #236'nın kalbi.
        if (IsPresent(accountStudentId))
            return StudentAccountBindingDecision.ConflictingStudent;

        return StudentAccountBindingDecision.Bind;
    }

    private static bool IsPresent(Guid? value) => value is not null && value != Guid.Empty;
}
