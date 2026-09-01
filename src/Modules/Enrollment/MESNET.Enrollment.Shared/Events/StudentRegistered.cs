namespace MESNET.Enrollment.Shared.Events;

public sealed record StudentRegistered(
    Guid StudentId,
    string FullName,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode,
    int ClassYear,
    string EducationType,
    string StudentNumber = "",
    // 3308 Madde 25: %50 oranı yalnız kalfalık yeterliğini kazanan MESEM 12. sınıf
    // öğrencilerine uygulanır. Payment bu bilgiyi başka modülün şemasından okuyamaz (#83).
    bool HasJourneymanQualification = false,
    // 3308 Madde 25 "yaşına uygun asgari ücret" ve aday çırak/çırak ayrımı Payment'ta gerekli;
    // öğrenci verisi Enrollment'ta ve modüller arası doğrudan sorgu yasak (#85).
    DateTime? BirthDate = null,
    string Category = "Student",
    /// <summary>
    /// Öğrencinin Keycloak kimliği (#230). Security modülü <c>UserAccount.StudentId</c>
    /// otoritesini bununla doldurur — o alan bugüne kadar HİÇ yazılmıyordu ve
    /// <c>student_id</c> claim'i doğrudan token'dan okunuyordu.
    ///
    /// <para><b>Sona eklendi ve varsayılanı var:</b> mevcut tüketiciler kırılmasın. Boş değer
    /// "eşleştirilemez" demektir ve tüketici sessizce atlar — uydurmaz.</para>
    /// </summary>
    Guid KeycloakUserId = default)
{
    /// <summary>
    /// Görünüm besleyen tüketicilerin <b>onarım</b> girdisine çevirir (#290).
    ///
    /// <para>Yön bilerek tek taraflıdır: kayıt olayı anlık görüntüye çevrilir, tersi
    /// <b>yoktur</b>. Ters çevrim olsaydı bir onarım yolu şube sayacını yeniden şişirebilirdi —
    /// düzeltilen hatanın ta kendisi.</para>
    /// </summary>
    public StudentSnapshotResynced ToSnapshot() => new(
        StudentId, FullName, InstitutionId, AcademicPeriodId, BranchCode, ClassYear,
        EducationType, StudentNumber, HasJourneymanQualification, BirthDate, Category,
        KeycloakUserId);
}
