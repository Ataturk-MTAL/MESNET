namespace MESNET.Common.Shared.Tenancy;

/// <summary>
/// Bir belgenin kiracı sınırına göre yeri. <b>Her belge tipi bilinçli olarak sınıflandırılır</b>
/// (#147) — sınıflandırılmamış belge bırakılamaz, test kırılır.
/// </summary>
public enum DocumentTenancy
{
    /// <summary>Kiracıya ait: <c>InstitutionId</c> taşır, iki okulun verisi birbirinden ayrıdır.</summary>
    Tenant,

    /// <summary>
    /// Bilinçli olarak kiracı sınırı DIŞINDA: ulusal katalog, ulusal parametre, kimlik katmanı
    /// ya da paylaşımlı işletme kataloğu. <b>Toptan <c>AllDocumentsAreMultiTenanted()</c>
    /// kullanılamamasının sebebi bu sınıftır</b> — kullanılsaydı bunlar da kiracı damgası alırdı.
    /// </summary>
    Shared,

    /// <summary>Kimlik katmanı — kiracıya <i>ait</i> değil, onu ya da kullanıcıyı tanımlar.</summary>
    Identity,

    /// <summary>
    /// <b>BOŞLUK.</b> Kiracıya ait veri taşıyor ama kiracı anahtarı YOK. Türetilmiş olduğu için
    /// göç engeli değildir (olaydan yeniden kurulur), ama çok-okul olduğunda <b>sızıntı
    /// yüzeyidir</b>: sorgu iki okulun satırını ayırt edemez.
    /// </summary>
    MissingKey,
}

/// <summary>
/// Belge tipi → kiracılık sınıfı. Marten'a kayıtlı her belge burada yer alır; eksik ya da fazla
/// giriş <c>DocumentTenancyDriftTests</c> ile kırılır.
/// </summary>
/// <remarks>
/// <para><b>Neden kodda:</b> "hangi belge kiracıya ait" sorusu kiracılık geçişinin (#149) sert
/// ön koşuludur ve dokümanda tutulursa kodla birlikte kaymaz. Burada durunca yeni bir belge
/// eklemek <b>kiracı/paylaşımlı kararını zorunlu kılar</b> — karar unutulamaz, build kırılır.</para>
///
/// <para>Sınıflandırma <c>Schema.For&lt;T&gt;()</c> bildirimlerinden ve canlı veri taramasından
/// çıkarıldı (#147). Tip adları dize tutulur: Common.Shared modül tiplerini referans edemez.</para>
/// </remarks>
public static class DocumentTenancyMap
{
    private const DocumentTenancy Tenant = DocumentTenancy.Tenant;
    private const DocumentTenancy Shared = DocumentTenancy.Shared;
    private const DocumentTenancy Identity = DocumentTenancy.Identity;
    private const DocumentTenancy MissingKey = DocumentTenancy.MissingKey;

    public static IReadOnlyDictionary<string, DocumentTenancy> All { get; } =
        new Dictionary<string, DocumentTenancy>(StringComparer.Ordinal)
    {
        // ── Kiracıya ait: InstitutionId taşır, kiracı sınırı içindedir ────────────────
        // Olay kaynaklı aggregate'ler — anlık görüntüleri de kiracıya aittir
        // #147 adım 1 ile kiracı anahtarı eklendi — önce MissingKey sınıfındaydılar
        ["AttendanceView"] = Tenant,
        ["StudentAbsenceView"] = Tenant,
        ["StudentNameView"] = Tenant,
        ["StudentPaymentProfile"] = Tenant,

        ["AttendanceRecord"] = Tenant,
        // Kademeli bildirim gönderim defteri (#247) — okulun kendi öğrencisine yaptığı tebligatın
        // kaydı; kiracı sınırını aşamaz.
        ["AbsenceNotificationLog"] = Tenant,
        // Saga durumu kiracıya aittir — süreç tek okulun içinde yürür
        ["PaymentSaga"] = Tenant,
        ["InternshipSaga"] = Tenant,
        ["PaidLeaveRequest"] = Tenant,

        ["AcademicPeriod"] = Tenant,
        ["AcademicPeriodView"] = Tenant,
        ["BranchStudentCountView"] = Tenant,
        ["BranchWorkloadConfig"] = Tenant,
        ["BusinessCoordinationView"] = Tenant,
        ["BusinessEvaluation"] = Tenant,
        ["ContractEmploymentView"] = Tenant,
        ["CoordinationConfig"] = Tenant,
        ["CoordinationPlacedStudentView"] = Tenant,
        ["GeneratedDocument"] = Tenant,
        ["GuidanceVisit"] = Tenant,
        ["InstitutionBranchView"] = Tenant,
        ["InternshipContract"] = Tenant,
        ["InternshipPlacement"] = Tenant,
        ["InternshipPlacementView"] = Tenant,
        ["InternshipSummary"] = Tenant,
        ["MonthlyActivityReport"] = Tenant,
        ["PaymentSummary"] = Tenant,
        ["PlacedStudentView"] = Tenant,
        ["PlacementView"] = Tenant,
        ["SchoolPlacedStudentView"] = Tenant,
        ["SkillExam"] = Tenant,
        ["StudentAttendanceReportView"] = Tenant,
        ["StudentPlacementReportView"] = Tenant,
        ["StudentProfile"] = Tenant,
        ["StudentTermGrade"] = Tenant,
        ["StudentTermGradeView"] = Tenant,
        ["TeacherProfile"] = Tenant,
        ["TeacherSchedule"] = Tenant,
        ["VisitAssignmentReportView"] = Tenant,
        ["WeeklyVisitAssignment"] = Tenant,
        ["WeeklyVisitPlan"] = Tenant,
        ["WorkCalendar"] = Tenant,
        ["WorkCalendarReportView"] = Tenant,

        // ── Paylaşımlı: bilinçli olarak kiracı sınırı DIŞINDA ─────────────────────────
        // İşletmenin alan yetkisi — işletme kimliğine ait, okula değil
        ["BusinessBranchAuthorizationView"] = Shared,
        // Paylaşımlı işletme kataloğundan türer
        ["BusinessContactReportView"] = Shared,
        // İşletme kimliği düzeyinde — paylaşımlı katalog
        ["BusinessPaymentProfile"] = Shared,
        // Paylaşımlı işletme kataloğunun kopyası — bir işletme birden çok okuldan öğrenci alır
        ["BusinessProfileView"] = Shared,
        // 3308 devlet katkısı oranları — ulusal parametre
        ["ClassYearContributionClaim"] = Shared,
        // PAYLAŞIMLI İŞLETME KATALOĞU — bir işletme birden çok okuldan öğrenci alır; tüm
        // okullar tüm işletmeleri listeler. Alan artık adıyla da bunu söylüyor:
        // Business.RegisteredByInstitutionId, PROVENANCE (hangi okul kaydetti) — kapsam değil
        // (ADR-0003 adım 4). Aynı vergi numaralı kayıtların birleştirilmesi ve okula bağlı
        // alanların ilişki entity'sine taşınması ayrı bir domain migration'dır.
        ["Business"] = Shared,

        // Kiracının KENDİSİ — kiracıya ait değil, onu tanımlar
        ["Institution"] = Identity,

        // MEB alan/dal kataloğu — ulusal, kuruma göre değişmez
        ["FieldOfStudy"] = Shared,
        // Kurum listesi görünümü — kiracıların KENDİSİ, kiracıya ait değil
        ["InstitutionView"] = Shared,
        // Yetki kapsamı yapılandırması — kimlik katmanı, kiracıdan bağımsız
        ["PermissionScopeConfig"] = Shared,
        // Asgari ücret ve 3308 oranları — kurum ÜSTÜ ulusal parametre katmanı
        ["SalaryCalculationConfig"] = Shared,
        // Devamsızlık sınırları da ulusal parametredir (#183): MEB Yönetmeliği md. 36'dan
        // türer, okul başına değişemez. Damgalanırsa her okul kendi sınırını belirlerdi.
        ["AttendanceLimitConfig"] = Shared,
        // Kullanıcı adı çözümlemesi — kimlik katmanı (Keycloak), kiracıya ait değil
        ["UserNameView"] = Shared,

        // ── Kiracının kendisi / kimlik katmanı ────────────────────────────────────────
        // Kimlik katmanı; InstitutionId taşır ama kiracı sınırı DEĞİL — kullanıcı kurumlar arası taşınabilir
        ["UserAccount"] = Identity,

        // Davet, kimlik katmanının ONBOARDING kenarıdır: daveti tamamlayan kişinin henüz
        // kullanıcı kaydı, dolayısıyla kiracısı YOKTUR — uç anonimdir. Kiracıya ait
        // sınıflandırılsaydı daveti okumak için önce kiracıyı bilmek gerekirdi; daveti okumadan
        // da kiracı bilinemez. Aynı döngüsellik UserAccount'ta da var (#149).
        // Ölçüldü: Tenant iken anonim POST /api/security/invitations/{id}/complete
        // DefaultTenantUsageDisabledException ile 500 döndü.
        // KALAN BORÇ: davet listeleme kapsamı isteğe bağlı InstitutionId filtresiyle çalışıyor
        // (InvitationHandler). Kiracılık artık o listeyi ARKADAN DESTEKLEMİYOR; kapsam kararı
        // sunucuda verilmeli.
        ["UserInvitation"] = Identity,

        // ── BOŞLUK: kiracıya ait veri taşıyıp kiracı anahtarı olmayan belge KALMADI ────
        // (Dördü de #147 adım 1 ile InstitutionId aldı; sınıf bilerek boş bırakıldı.)

    };
}
