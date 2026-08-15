namespace MESNET.Enrollment.Core.Policies;

/// <summary>
/// Öğrenci numarasının <c>StudentProfile</c> doğal anahtarındaki rolü (#237).
///
/// <para><b>Neden gerekti:</b> <c>StudentProfile</c>'ın tekillik anahtarı <b>hiç yoktu</b>.
/// <c>RegisterStudentHandler</c> her çağrıda <c>Guid.NewGuid()</c> üretip yazıyor, Persistence
/// katmanında tek bir <c>IsUnique</c> bulunmuyordu; aynı öğrenci N kez kaydedilirse N satır
/// doğuyordu. #204'te ölçüldü: <b>122 öğrenci → 774 kayıt</b>. O issue istemci tarafını
/// (sayfalama) düzeltti, sunucudaki kök neden kaldı — yani hata farklı bir çağırandan
/// tekrar üretilebilirdi.</para>
///
/// <para><b>Anahtar neden <c>(AcademicPeriodId, StudentNumber)</c>:</b> öğrenci numarası okul
/// içinde ve dönem içinde benzersizdir; iki okulun aynı numarayı kullanması <b>normaldir</b>.
/// Akademik dönem tek bir okula ait olduğu için anahtar okulu da kapsar; kiracı damgası bunu
/// ayrıca garantiler.</para>
///
/// <para><b>Neden normalleştirme:</b> anahtar ham değer üzerine kurulsaydı <c>" 1101"</c> ile
/// <c>"1101"</c> iki ayrı satır olurdu ve kısıt hiçbir şey korumazdı. <c>TaxNumberPolicy</c>
/// (#150) aynı gerekçeyle aynı kararı veriyor.</para>
/// </summary>
public static class StudentNumberPolicy
{
    /// <summary>
    /// Anahtara girecek biçime çevirir. Boş/yalnız boşluk değer <c>null</c> döner — "numarası
    /// girilmemiş" ile "numarası boş dize" aynı şeydir.
    /// </summary>
    public static string? Normalize(string? studentNumber) =>
        string.IsNullOrWhiteSpace(studentNumber) ? null : studentNumber.Trim();

    /// <summary>
    /// Bu öğrenci tekillik kontrolüne tabi mi.
    ///
    /// <para><b>Numara zorunlu değildir</b> (bugün <c>string?</c>, doğrulayıcı yalnız uzunluğa
    /// bakıyor) ve zorunlu kılmak mevcut kayıtları kırardı. Bu yüzden tekillik <b>kısmi</b>
    /// uygulanır: numarasız öğrenciler kısıtın dışındadır ve birbirleriyle çakışmazlar.
    /// Veritabanı tarafında bunun karşılığı kısmi index'tir (<c>Predicate</c>).</para>
    /// </summary>
    public static bool RequiresUniqueness(string? studentNumber) => Normalize(studentNumber) is not null;
}
