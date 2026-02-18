namespace MESNET.Common.Shared.Security;

/// <summary>
/// MESNET Phase 1 izin sabitleri.
/// Her izin "kaynak:eylem" formatındadır.
/// Wildcard desteği: "student:*" → student altındaki tüm izinleri kapsar.
/// </summary>
public static class Permissions
{
    /// <summary>
    /// Kurum (okul) yönetimine ilişkin izinler.
    /// Yönetici ve müdür rollerine atanır.
    /// </summary>
    public static class Institution
    {
        /// <summary>Kurum bilgilerini görüntüleme.</summary>
        public const string View = "institution:view";

        /// <summary>Kurum oluşturma, güncelleme ve yapılandırma.</summary>
        public const string Manage = "institution:manage";

        /// <summary>Kurum kaydını silme (geri alınamaz).</summary>
        public const string Delete = "institution:delete";

        /// <summary>Kurum personelini yetkilendirme ve rol atama.</summary>
        public const string Staff = "institution:staff:manage";

        /// <summary>Kuruma ait raporları görüntüleme.</summary>
        public const string Report = "institution:report:view";
    }

    /// <summary>
    /// Öğrenci profillerine ilişkin izinler.
    /// Kayıt, güncelleme ve görüntüleme işlemlerini kapsar.
    /// </summary>
    public static class Student
    {
        /// <summary>Tüm öğrenci profillerini görüntüleme.</summary>
        public const string View = "student:view";

        /// <summary>Öğrenci kaydı oluşturma ve güncelleme.</summary>
        public const string Manage = "student:manage";

        /// <summary>Öğrencinin kendi profilini görüntüleme.</summary>
        public const string ViewOwn = "student:view-own";

        /// <summary>Öğrencinin kendi profilini güncelleme.</summary>
        public const string UpdateOwn = "student:update-own";

        /// <summary>Öğrencinin devamsızlık kayıtlarını yönetme.</summary>
        public const string Attendance = "student:attendance:manage";

        /// <summary>Öğrencinin maaş/ücret bilgilerini yönetme.</summary>
        public const string Salary = "student:salary:manage";
    }

    // /// <summary>
    // /// Staj protokolü (MEB protokolü) işlemlerine ilişkin izinler.
    // /// Phase 2'de protokol modülü eklendiğinde aktif edilecek.
    // /// </summary>
    // public static class Protocol
    // {
    //     /// <summary>Protokol belgelerini görüntüleme.</summary>
    //     public const string View = "protocol:view";
    //
    //     /// <summary>Yeni protokol oluşturma.</summary>
    //     public const string Create = "protocol:create";
    //
    //     /// <summary>Protokolü onaylama veya reddetme.</summary>
    //     public const string Approve = "protocol:approve";
    //
    //     /// <summary>Protokol düzenleme ve tam yönetim.</summary>
    //     public const string Manage = "protocol:manage";
    //
    //     /// <summary>Protokol eğitim programını düzenleme.</summary>
    //     public const string Program = "protocol:program:manage";
    // }

    /// <summary>
    /// İşletme (staj yeri) yönetimine ilişkin izinler.
    /// </summary>
    public static class Company
    {
        /// <summary>İşletme bilgilerini görüntüleme.</summary>
        public const string View = "company:view";

        /// <summary>İşletme oluşturma, güncelleme, onay/red ve durum değişikliği.</summary>
        public const string Manage = "company:manage";

        /// <summary>İşletmeye ait belgeleri (sicil, vergi vb.) yönetme.</summary>
        public const string Document = "company:document:manage";

        /// <summary>İşletmedeki öğrenci listesini yönetme.</summary>
        public const string Student = "company:student:manage";

        /// <summary>İşletme ziyaret planlaması ve takibi.</summary>
        public const string Visit = "company:visit:manage";

        /// <summary>İşletmenin öğrenci talebinde bulunması.</summary>
        public const string RequestStudent = "company:student:request";

        /// <summary>İşletme devamsızlık çizelgesini yönetme.</summary>
        public const string Attendance = "company:attendance:manage";

        /// <summary>İşletmenin ödeme dekontu yüklemesi.</summary>
        public const string UploadReceipt = "company:receipt:upload";

        /// <summary>Usta öğretici belgesi yönetimi.</summary>
        public const string MasterTrainer = "company:trainer:manage";
    }

    /// <summary>
    /// Staj sürecine ilişkin izinler.
    /// Başvuru, onay, sözleşme ve fesih işlemlerini kapsar.
    /// </summary>
    public static class Internship
    {
        /// <summary>Öğrencinin staj başvurusu yapması.</summary>
        public const string Apply = "internship:apply";

        /// <summary>Tüm staj kayıtlarını görüntüleme (yetkili personel için).</summary>
        public const string View = "internship:view";

        /// <summary>Staj başvurularını inceleme ve ön değerlendirme.</summary>
        public const string Review = "internship:review";

        /// <summary>Staj yerleştirme ve fesih onay zincirini ilerletme.</summary>
        public const string Approve = "internship:approve";

        /// <summary>Öğrencinin kendi staj durumunu görüntülemesi.</summary>
        public const string ViewOwn = "internship:view-own";

        /// <summary>Staj sürecini yönetme (transfer, askı, fesih talebi vb.).</summary>
        public const string Manage = "internship:manage";

        /// <summary>Staj sözleşmesi oluşturma, imzalama ve aktifleştirme.</summary>
        public const string Contract = "internship:contract:manage";

        /// <summary>Staj raporlarını oluşturma ve yönetme.</summary>
        public const string Report = "internship:report:manage";
    }

    /// <summary>
    /// Devamsızlık takibine ilişkin izinler.
    /// </summary>
    public static class Attendance
    {
        /// <summary>Devamsızlık kayıtlarını görüntüleme.</summary>
        public const string View = "attendance:view";

        /// <summary>Öğrencinin kendi devamsızlık kaydını görüntülemesi.</summary>
        public const string ViewOwn = "attendance:view-own";

        /// <summary>Devamsızlık kaydı oluşturma ve düzeltme.</summary>
        public const string Manage = "attendance:manage";

        /// <summary>Devamsızlık raporları oluşturma.</summary>
        public const string Report = "attendance:report";

        /// <summary>Devamsızlık belgesi (sağlık raporu vb.) yükleme.</summary>
        public const string Upload = "attendance:upload";

        /// <summary>Devamsızlık kaydını doğrulama ve onaylama.</summary>
        public const string Approve = "attendance:approve";
    }

    /// <summary>
    /// Maaş/ücret ödeme sürecine ilişkin izinler.
    /// Dekont yükleme, onay zinciri ve parametre yönetimini kapsar.
    /// </summary>
    public static class Salary
    {
        /// <summary>Tüm ödeme kayıtlarını görüntüleme.</summary>
        public const string View = "salary:view";

        /// <summary>Öğrencinin kendi ödeme bilgilerini görüntülemesi.</summary>
        public const string ViewOwn = "salary:view-own";

        /// <summary>Maaş hesaplama işlemini başlatma.</summary>
        public const string Calculate = "salary:calculate";

        /// <summary>Dekont onay zincirini ilerletme (öğretmen/müdür yardımcısı).</summary>
        public const string Approve = "salary:approve";

        /// <summary>Dekont yükleme ve öğrenci onayı yönetimi.</summary>
        public const string Receipt = "salary:receipt:manage";

        /// <summary>Asgari ücret ve hesaplama parametrelerini güncelleme.</summary>
        public const string Parameter = "salary:parameter:manage";
    }

    /// <summary>
    /// Koordinatör öğretmene ilişkin izinler.
    /// Ders programı, işletme ziyareti ve faaliyet raporlarını kapsar.
    /// </summary>
    public static class Coordinator
    {
        /// <summary>Koordinatör atama işlemi.</summary>
        public const string Assign = "coordinator:assign";

        /// <summary>Koordinatör ders programı oluşturma ve güncelleme.</summary>
        public const string Schedule = "coordinator:schedule:manage";

        /// <summary>İşletme rehberlik ziyareti ve değerlendirme yönetimi.</summary>
        public const string Visit = "coordinator:visit:manage";

        /// <summary>Aylık faaliyet raporu ve beceri sınavı raporu yönetimi.</summary>
        public const string Report = "coordinator:report:manage";

        /// <summary>İşletme ve öğrenci ile koordinatör iletişimi.</summary>
        public const string Communication = "coordinator:communication";
    }

    /// <summary>
    /// Alan şefi / bölüm başkanına ilişkin izinler.
    /// Öğretmen atama ve iş yükü dağıtımını kapsar.
    /// </summary>
    public static class DepartmentHead
    {
        /// <summary>Koordinatör öğretmen iş yükü dağıtımı.</summary>
        public const string Distribution = "department:distribution:manage";

        /// <summary>Öğretmen iş yükünü görüntüleme.</summary>
        public const string Workload = "department:workload:view";

        /// <summary>Öğretmeni koordinatör olarak atama.</summary>
        public const string TeacherAssign = "department:teacher:assign";

        /// <summary>Koordinatör ders programlarını görüntüleme.</summary>
        public const string ScheduleView = "department:schedule:view";
    }

    /// <summary>
    /// Belge yaşam döngüsüne ilişkin izinler.
    /// PDF üretimi, yazdırma, imzalama ve arşivlemeyi kapsar.
    /// </summary>
    public static class Document
    {
        /// <summary>Üretilmiş belgeleri görüntüleme ve indirme.</summary>
        public const string View = "document:view";

        /// <summary>Belge yükleme (ıslak imzalı sözleşme, fesih belgesi vb.).</summary>
        public const string Upload = "document:upload";

        /// <summary>Belgeyi onaylama ve arşivleme.</summary>
        public const string Approve = "document:approve";

        /// <summary>Belge QR/barkod tarama ve doğrulama.</summary>
        public const string Scan = "document:scan";

        /// <summary>İmzalanıp iade edilen belgeyi işaretleme.</summary>
        public const string Verify = "document:verify";

        /// <summary>Belgenin yazdırıldığını ve teslimini takip etme.</summary>
        public const string Track = "document:track";
    }

    /// <summary>
    /// İletişim ve sorun bildirimi izinleri.
    /// </summary>
    public static class Communication
    {
        /// <summary>Mesaj gönderme.</summary>
        public const string SendMessage = "communication:send";

        /// <summary>Mesajları görüntüleme.</summary>
        public const string ViewMessages = "communication:view";

        /// <summary>Sorun/şikayet bildirimi oluşturma.</summary>
        public const string ReportIssue = "communication:issue:report";

        /// <summary>Bildirilen sorunları yönetme ve kapatma.</summary>
        public const string ManageIssues = "communication:issue:manage";
    }

    /// <summary>
    /// Kullanıcı hesabı yönetimine ilişkin izinler.
    /// Keycloak entegrasyonlu kullanıcı CRUD ve rol atamalarını kapsar.
    /// </summary>
    public static class UserManagement
    {
        /// <summary>Kullanıcı listesini ve profillerini görüntüleme.</summary>
        public const string View = "user:view";

        /// <summary>Yeni kullanıcı oluşturma veya davet gönderme.</summary>
        public const string Create = "user:create";

        /// <summary>Kullanıcı bilgilerini güncelleme ve aktif/pasif yapma.</summary>
        public const string Update = "user:update";

        /// <summary>Kullanıcı hesabını silme.</summary>
        public const string Delete = "user:delete";

        /// <summary>Kullanıcıya rol ve özel izin atama.</summary>
        public const string RolesManage = "user:roles:manage";

        /// <summary>Davet talebini onaylama ve Keycloak hesabı açma.</summary>
        public const string Approve = "user:approve";
    }

    /// <summary>
    /// Tüm permission sabitlerini reflection ile toplar.
    /// Policy oluşturma ve UI listeleme için kullanılır.
    /// </summary>
    public static IReadOnlyList<string> GetAll()
    {
        return typeof(Permissions)
            .GetNestedTypes()
            .SelectMany(t => t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();
    }
}
