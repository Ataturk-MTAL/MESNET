namespace MESNET.Audit.Core.Services;

/// <summary>
/// Komut tipinin Türkçe arayüz etiketi.
/// </summary>
/// <remarks>
/// <para><b>Neden sunucuda:</b> arayüz kendi eşleme tablosunu tutsaydı, yeni bir komut
/// eklendiğinde denetim listesinde sessizce ham tip adı ("MarkAttendance") belirirdi ve
/// bunu ne derleyici ne bir test görebilirdi.</para>
///
/// <para><b>Sözlük KISMİDİR ve bu bilinçlidir.</b> Eşleşmeyen komut ham adıyla görünür —
/// satır kaybolmaz, yalnız etiketi çevrilmemiştir. Alternatifi 200 satırlık bir tabloyu
/// her komut eklendiğinde kırmızıya çeviren bir kilit olurdu; o kilit denetim izinin
/// kendisini geciktirirdi.</para>
///
/// <para><b>Anahtarlar yalnız tip adıdır, tam nitelikli ad değil.</b> <c>RequestTermination</c>
/// Business, Contract ve Internship modüllerinin üçünde de tanımlıdır ama kavramsal olarak
/// aynı eylemi (fesih talebi) temsil eder — sözlükte tek girişle karşılanır.</para>
/// </remarks>
public static class AuditCommandLabels
{
    public static IReadOnlyDictionary<string, string> All { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // ── Kurum ─────────────────────────────────────────────────────
            ["ActivateBranch"] = "Alan aktifleştirildi",
            ["AuthorizeStaff"] = "Personel yetkilendirildi",
            ["CloseAcademicPeriod"] = "Akademik dönem kapatıldı",
            ["CreateAcademicPeriod"] = "Akademik dönem oluşturuldu",
            ["CreateInstitution"] = "Kurum oluşturuldu",
            ["DeactivateBranch"] = "Alan pasifleştirildi",
            ["RebuildInstitutionHierarchy"] = "Kurum ağacı yeniden kuruldu",
            ["ResyncStaffBranchCodes"] = "Personel alan kodları yeniden eşitlendi",
            ["SetGradeEntryWindow"] = "Not giriş penceresi belirlendi",
            ["SetInstitutionBrandPalette"] = "Kurum marka paleti değiştirildi",
            ["UpdateBranchSpecializations"] = "Alan uzmanlıkları güncellendi",
            ["UpdateBranchSupervisorConfig"] = "Alan şefi yapılandırması güncellendi",
            ["UpdateInstitution"] = "Kurum bilgileri güncellendi",
            ["UpdateScheduleConfiguration"] = "Ders programı yapılandırması güncellendi",

            // ── İşletme ───────────────────────────────────────────────────
            ["ActivateBusiness"] = "İşletme aktifleştirildi",
            ["ApproveBusiness"] = "İşletme onaylandı",
            ["ApproveDocument"] = "İşletme belgesi onaylandı",
            ["AuthorizeBusinessForBranches"] = "İşletme alanlara yetkilendirildi",
            ["CloseBusiness"] = "İşletme kapatıldı",
            ["DeactivateBusiness"] = "İşletme pasifleştirildi",
            ["DeleteBusinessDocument"] = "İşletme belgesi silindi",
            ["DeleteInstructorDocument"] = "Usta öğretici belgesi silindi",
            ["InvalidateInstructorDocument"] = "Usta öğretici belgesi geçersiz kılındı",
            ["RegisterBusiness"] = "İşletme kaydedildi",
            ["RejectBusiness"] = "İşletme reddedildi",
            ["RequestInstructorDocument"] = "Usta öğretici belgesi istendi",
            ["ResyncBusinessProjections"] = "İşletme görünümleri yeniden eşitlendi",
            ["RetractBusinessClosure"] = "İşletme kapatması geri alındı",
            ["SelfRegisterBusiness"] = "İşletme kendi kaydını oluşturdu",
            ["SuspendBusiness"] = "İşletme askıya alındı",
            ["UpdateBusinessInfo"] = "İşletme bilgileri güncellendi",
            ["UpdateCapacity"] = "İşletme kapasitesi güncellendi",
            ["UploadDocument"] = "İşletme belgesi yüklendi",
            ["UploadInstructorDocument"] = "Usta öğretici belgesi yüklendi",

            // ── Kayıt / Yerleştirme (Enrollment) ─────────────────────────
            ["ApplyForInternship"] = "Staj başvurusu yapıldı",
            ["BackfillBusinessBranchAuthorizations"] = "İşletme alan yetkileri geriye dönük dolduruldu",
            ["DeregisterStudent"] = "Öğrenci kaydı silindi",
            ["MarkAsFailedToComplete"] = "Yerleştirme tamamlanamadı olarak işaretlendi",
            ["PlaceStudent"] = "Öğrenci yerleştirildi",
            ["RegisterStudent"] = "Öğrenci kaydedildi",
            ["RegisterTeacher"] = "Öğretmen kaydedildi",
            ["RequestStudent"] = "Öğrenci talep edildi",
            ["ResyncPlacementProjections"] = "Yerleştirme görünümleri yeniden eşitlendi",
            ["ResyncStudentProjections"] = "Öğrenci görünümleri yeniden eşitlendi",
            ["SyncStudentCounts"] = "Öğrenci sayıları eşitlendi",
            ["UpdateStudentProfile"] = "Öğrenci profili güncellendi",

            // ── Sözleşme ──────────────────────────────────────────────────
            ["ActivateContract"] = "Sözleşme yürürlüğe girdi",
            ["CompleteContract"] = "Sözleşme tamamlandı",
            ["CreateContract"] = "Sözleşme oluşturuldu",
            ["RejectTermination"] = "Fesih reddedildi",
            // Business / Contract / Internship üçünde de tanımlıdır — tek kavram, tek etiket.
            ["RequestTermination"] = "Fesih talep edildi",
            ["ResumeContract"] = "Sözleşme yeniden başlatıldı",
            ["ResyncInternshipLinks"] = "Staj bağlantıları yeniden eşitlendi",
            ["SignContract"] = "Sözleşme imzalandı",
            ["SubmitContractForSignature"] = "Sözleşme imzaya sunuldu",
            ["SuspendContract"] = "Sözleşme askıya alındı",
            ["TerminateContract"] = "Sözleşme feshedildi",
            ["UploadContractDocument"] = "Sözleşme belgesi yüklendi",

            // ── Devamsızlık ───────────────────────────────────────────────
            ["ApproveAttendance"] = "Devamsızlık onaylandı",
            ["ApproveHealthReport"] = "Sağlık raporu onaylandı",
            ["ApprovePaidLeave"] = "Ücretli izin okulca onaylandı",
            ["AttachHealthReport"] = "Sağlık raporu yüklendi",
            ["BusinessApprovePaidLeave"] = "Ücretli izin işletmece onaylandı",
            ["CorrectAttendance"] = "Devamsızlık düzeltildi",
            ["DeleteAttendance"] = "Devamsızlık silindi",
            ["MarkAttendance"] = "Devamsızlık girildi",
            ["NotifyAttendancePendingApproval"] = "Onay bekleyen devamsızlık bildirildi",
            ["RejectHealthReport"] = "Sağlık raporu reddedildi",
            ["RejectPaidLeave"] = "Ücretli izin reddedildi",
            ["RequestPaidLeave"] = "Ücretli izin talep edildi",
            ["ResyncAttendanceSnapshots"] = "Devamsızlık anlık görüntüleri yeniden eşitlendi",
            ["UpdateAbsenceLimits"] = "Devamsızlık limitleri güncellendi",
            ["UpdateWorkCalendar"] = "Çalışma takvimi güncellendi",
            ["VerifyAttendance"] = "Devamsızlık doğrulandı",

            // ── Maaş / dekont ─────────────────────────────────────────────
            ["ApproveReceiptByDeputy"] = "Dekont müdür yardımcısınca onaylandı",
            ["ApproveReceiptByTeacher"] = "Dekont koordinatör öğretmence onaylandı",
            ["CalculateMonthlySalary"] = "Aylık ücret hesaplandı",
            ["ConfirmSalary"] = "Ücret onaylandı",
            ["OpenMonthlySalaryPeriods"] = "Aylık ücret dönemleri açıldı",
            ["RecalculateMonthlySalary"] = "Aylık ücret yeniden hesaplandı",
            ["RejectReceipt"] = "Dekont reddedildi",
            ["UpdateMinimumWage"] = "Asgari ücret güncellendi",
            ["UploadReceiptByBusiness"] = "Dekont işletmece yüklendi",
            ["UploadReceiptByStudent"] = "Dekont öğrencice yüklendi",

            // ── Koordinasyon ──────────────────────────────────────────────
            ["AddWeeklyVisitAssignment"] = "Haftalık ziyaret ataması eklendi",
            ["ApproveGuidanceVisit"] = "Rehberlik ziyareti onaylandı",
            ["ApproveMonthlyActivityReport"] = "Aylık faaliyet raporu onaylandı",
            ["AssignBusinessToFreeSlot"] = "İşletme boş saate atandı",
            ["AssignBusinessToTeacher"] = "İşletme öğretmene atandı",
            ["CreateBusinessEvaluation"] = "İşletme değerlendirmesi oluşturuldu",
            ["CreateGuidanceVisit"] = "Rehberlik ziyareti oluşturuldu",
            ["CreateMonthlyActivityReport"] = "Aylık faaliyet raporu oluşturuldu",
            ["CreateSkillExam"] = "Beceri sınavı oluşturuldu",
            ["DeleteWeeklyVisitAssignment"] = "Haftalık ziyaret ataması silindi",
            ["DeleteWeeklyVisitPlan"] = "Haftalık ziyaret planı silindi",
            ["EnterSchoolTermGrade"] = "Dönem notu okulda staj için girildi",
            ["EnterStudentTermGrade"] = "Dönem notu işletmece girildi",
            ["GenerateWeeklyVisits"] = "Haftalık ziyaretler oluşturuldu",
            ["RecalculateDistances"] = "Mesafeler yeniden hesaplandı",
            ["ResyncCoordinationViews"] = "Koordinasyon görünümleri yeniden eşitlendi",
            ["ResyncWeeklyVisitEvents"] = "Haftalık ziyaret olayları yeniden eşitlendi",
            ["SetBusinessManualDistance"] = "İşletme mesafesi elle belirlendi",
            ["SubmitGuidanceVisit"] = "Rehberlik ziyareti gönderildi",
            ["SubmitMonthlyActivityReport"] = "Aylık faaliyet raporu gönderildi",
            ["SubmitSchoolTermGrade"] = "Dönem notu okulda staj için gönderildi",
            ["SubmitStudentTermGrade"] = "Dönem notu işletmece gönderildi",
            ["UnassignBusinessFromTeacher"] = "İşletme öğretmen atamasından kaldırıldı",
            ["UnassignBusinessSlot"] = "İşletme saat ataması kaldırıldı",
            ["UpdateBranchAssignedHours"] = "Alana ayrılan saatler güncellendi",
            ["UpdateBusinessAssignedHours"] = "İşletmeye ayrılan saatler güncellendi",
            ["UpdateBusinessEvaluation"] = "İşletme değerlendirmesi güncellendi",
            ["UpdateGuidanceVisit"] = "Rehberlik ziyareti güncellendi",
            ["UpdateMonthlyActivityReport"] = "Aylık faaliyet raporu güncellendi",
            ["UpdateSkillExam"] = "Beceri sınavı güncellendi",
            ["UpsertBranchWorkloadConfig"] = "Alan iş yükü yapılandırması kaydedildi",
            ["UpsertCoordinationConfig"] = "Koordinasyon yapılandırması kaydedildi",
            ["UpsertTeacherSchedule"] = "Öğretmen ders programı kaydedildi",

            // ── Staj (Internship saga) ───────────────────────────────────
            ["ApproveTerminationByDeputy"] = "Fesih müdür yardımcısınca onaylandı",
            ["ApproveTerminationByDirector"] = "Fesih müdürce onaylandı",
            ["ApproveTerminationByTeacher"] = "Fesih koordinatör öğretmence onaylandı",
            ["CompleteInternshipContract"] = "Staj tamamlandı",
            ["LinkInternshipContract"] = "Staj sözleşmeyle bağlandı",
            ["OverrideTerminationApproval"] = "Fesih onayı yönetici kararıyla atlandı",
            ["ResyncInternshipSagas"] = "Staj sagaları yeniden eşitlendi",
            ["TerminateInternshipContract"] = "Staj feshedildi",

            // ── Raporlama ─────────────────────────────────────────────────
            ["DeleteDocument"] = "Belge silindi",
            ["DeleteDocumentsBatch"] = "Belgeler toplu silindi",
            ["DownloadDocumentsZip"] = "Belgeler zip olarak indirildi",
            ["GenerateAttendanceSheetDocument"] = "Devam çizelgesi belgesi üretildi",
            ["GenerateBatchDocuments"] = "Belgeler toplu üretildi",
            ["GenerateBusinessEvaluationDocument"] = "İşletme değerlendirme belgesi üretildi",
            ["GenerateGuidanceVisitBatchDocument"] = "Rehberlik ziyareti belgeleri toplu üretildi",
            ["GenerateGuidanceVisitDocument"] = "Rehberlik ziyareti belgesi üretildi",
            ["GenerateInternshipContractDocument"] = "Staj sözleşmesi belgesi üretildi",
            ["GenerateMonthlyActivityDocument"] = "Aylık faaliyet belgesi üretildi",
            ["GenerateMonthlyAttendanceBatchDocument"] = "Aylık devam belgeleri toplu üretildi",
            ["GenerateMonthlyAttendanceBatchPreview"] = "Aylık devam belgeleri toplu önizlendi",
            ["GenerateMonthlyAttendanceDocument"] = "Aylık devam belgesi üretildi",
            ["GenerateMonthlyAttendancePreview"] = "Aylık devam belgesi önizlendi",
            ["GenerateSkillExamDocument"] = "Beceri sınavı belgesi üretildi",
            ["GenerateTermGradeSlipDocument"] = "Dönem not fişi üretildi",
            ["GenerateTermGradeSlipFromGrades"] = "Dönem not fişi notlardan üretildi",
            ["GenerateTermGradeSlipPreview"] = "Dönem not fişi önizlendi",
            ["MarkDocumentAsArchived"] = "Belge arşivlendi olarak işaretlendi",
            ["MarkDocumentAsPrinted"] = "Belge yazdırıldı olarak işaretlendi",
            ["MarkDocumentAsSignedAndReturned"] = "Belge imzalı döndü olarak işaretlendi",
            ["NotifyDocumentDeleted"] = "Belge silindi bildirimi gönderildi",
            ["NotifyDocumentGenerated"] = "Belge üretildi bildirimi gönderildi",
            ["NotifyDocumentStatusChanged"] = "Belge durumu değişti bildirimi gönderildi",

            // ── Kullanıcı ve yetki ────────────────────────────────────────
            ["ApproveInvitation"] = "Davet onaylandı",
            ["ChangeUserBranches"] = "Kullanıcının alanları değiştirildi",
            ["ChangeUserBusiness"] = "Kullanıcının işletmesi değiştirildi",
            ["ChangeUserInstitution"] = "Kullanıcının kurumu değiştirildi",
            ["ChangeUserPermissions"] = "Kullanıcı izinleri değiştirildi",
            ["ChangeUserRoles"] = "Kullanıcı rolleri değiştirildi",
            ["ChangeUserStudents"] = "Kullanıcının öğrenci bağı değiştirildi",
            ["CompleteInvitation"] = "Davet tamamlandı",
            ["CreateInvitation"] = "Davet oluşturuldu",
            ["SetActiveInstitution"] = "Aktif kurum bağlamı değiştirildi",
            ["CreateUser"] = "Kullanıcı oluşturuldu",
            ["DeleteUser"] = "Kullanıcı silindi",
            ["PurgeKeycloakInstitutionAttribute"] = "Keycloak kurum özniteliği temizlendi",
            ["RejectInvitation"] = "Davet reddedildi",
            ["ResendInvitation"] = "Davet yeniden gönderildi",
            ["ResyncUserDisplayNames"] = "Kullanıcı görünen adları yeniden eşitlendi",
            ["SyncUsersFromKeycloak"] = "Kullanıcılar Keycloak'tan eşitlendi",
            ["ToggleUserStatus"] = "Kullanıcı durumu değiştirildi",
            ["UpdatePermissionScopes"] = "İzin kapsamları güncellendi",
            ["UpdateUser"] = "Kullanıcı bilgileri güncellendi",
        };

    /// <summary>
    /// Komutun Türkçe etiketi; sözlükte yoksa <b>ham tip adı</b>. Boş dönmez — boş bir etiket
    /// listede boş hücre demek olurdu ve satır okunamaz hâle gelirdi.
    /// </summary>
    public static string For(string commandType)
        => All.TryGetValue(commandType, out var label) ? label : commandType;
}
