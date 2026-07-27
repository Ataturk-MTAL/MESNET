namespace MESNET.Institution.Application.Commands;

/// <summary>
/// Personel kayıtlarındaki alan (branş) bilgisini olay olarak yeniden yayınlar (#126).
///
/// <para><b>İkincil geçiş adımıdır, birincil yol değildir.</b> Alan bilgisi normalde
/// kullanıcı <b>kayıt sırasında</b> girilir (<c>CreateUser.BranchCodes</c>). Bu komut
/// yalnız #126 öncesinde oluşturulmuş kullanıcıların kapsamını, personel kaydında
/// zaten bulunan güvenilir bilgiden doldurmak içindir.</para>
///
/// <para><b>Uydurmaz:</b> yalnız <c>BranchCode</c> gerçekten dolu olan personel için olay
/// yayınlar. Branşı olmayan personel (müdür, müdür yardımcısı) atlanır — bu beklenen
/// normal durumdur, raporlanacak bir eksik değildir. Alanı belirsiz kalan kullanıcılar
/// kullanıcı yönetimi ekranında "branş atanmamış" olarak listelenir ve idare elle girer.</para>
///
/// <para>Tekrar çalıştırmak güvenlidir (idempotent).</para>
/// </summary>
public sealed record ResyncStaffBranchCodes;

/// <param name="TotalStaff">İncelenen personel kaydı sayısı.</param>
/// <param name="Published">Alan bilgisi bulunup olay yayınlanan personel sayısı.</param>
/// <param name="SkippedNoBranch">Alanı olmayan personel — beklenen durum, eksik değil.</param>
/// <param name="SkippedNoKeycloakId">Keycloak kimliği bulunmayan personel — eşleştirilemez.</param>
public sealed record ResyncStaffBranchCodesResult(
    int TotalStaff,
    int Published,
    int SkippedNoBranch,
    int SkippedNoKeycloakId);
