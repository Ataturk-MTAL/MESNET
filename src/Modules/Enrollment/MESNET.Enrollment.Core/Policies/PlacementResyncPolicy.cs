using System.Diagnostics.CodeAnalysis;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Core.Policies;

/// <summary>
/// Yerleştirme projeksiyonları yeniden yayınlanırken hangi kaydın <b>atlanacağı</b> (#185).
/// Saf fonksiyon — G/Ç yapmaz.
/// </summary>
/// <remarks>
/// <para><c>StudentPlaced</c> olayını dört tüketici dinliyor ve <c>StudentName</c>,
/// <c>BusinessName</c>, <c>BranchName</c> alanlarını denormalize tutuyor. Kaynak kayıt eksikken
/// yayınlamak, o tüketicilerin verisini boş dizeyle <b>ezer</b> — bu yüzden eksik kayıt atlanır.</para>
///
/// <para><b>Tuzak:</b> "işletme kaydı yoksa atla" kuralı <see cref="PlacementTypePolicy.IsSchoolBased"/>
/// olan yerleştirmeye uygulanamaz. Okulda stajda işletme <b>yoktur ve bu eksik veri değildir</b>
/// (#159). Koşul <c>student is null || business is null</c> diye sadeleştirilirse okulda staj
/// yapan her öğrenci sessizce atlanır: <c>SchoolPlacedStudentView</c> hiç dolmaz, öğrenci not
/// giriş listesinde görünmez ve dönem notu girilemez — yani #171 sıfır sinyalle geri kırılır.</para>
///
/// <para>Bu yüzden karar burada adlandırıldı ve testle kilitlendi; tek satırlık bir koşul olarak
/// handler'ın içinde kalsaydı sadeleştirilmesi fark edilmezdi.</para>
/// </remarks>
public static class PlacementResyncPolicy
{
    /// <summary>
    /// Yeniden yayın atlanmalı mı?
    /// </summary>
    /// <param name="hasStudent">Öğrenci profili yüklenebildi mi.</param>
    /// <param name="businessId">Yerleştirmenin işletmesi — okulda stajda <c>null</c> (#159).</param>
    /// <param name="hasBusiness">İşletme görünümü yüklenebildi mi.</param>
    public static bool ShouldSkip(bool hasStudent, Guid? businessId, bool hasBusiness)
    {
        // Öğrenci adı her yerleştirmede gerekir — türü ne olursa olsun.
        if (!hasStudent) return true;

        // İşletme yalnız işletmeli yerleştirmede aranır. Okulda stajda yokluğu normaldir.
        return !PlacementTypePolicy.IsSchoolBased(businessId) && !hasBusiness;
    }

    /// <summary>
    /// Çağıran için tipli sarmalayıcı. <c>false</c> döndüğünde <paramref name="student"/>'ın
    /// dolu olduğunu derleyiciye bildirir; aksi hâlde çağıranın atlama kuralını ve null
    /// kontrolünü <b>ikiye ayırması</b> gerekirdi ve kural iki yerde yaşamaya başlardı.
    /// </summary>
    public static bool ShouldSkip(
        [NotNullWhen(false)] StudentProfile? student, Guid? businessId, bool hasBusiness)
        => ShouldSkip(student is not null, businessId, hasBusiness);
}
