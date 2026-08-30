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

        /// <summary>Dönem sonu not giriş penceresini açma/kapatma (rolden bağımsız — yetkili olan herkes).</summary>
        public const string ManageGradeWindow = "institution:grade-window:manage";

        /// <summary>
        /// Kurum genelinde <b>tüm alanların</b> koordinasyon verisine yazma muafiyeti (#126).
        ///
        /// <para>Bu izin erişim değil <b>kapsam</b> açar: sahibi, kendi alan(lar)ına bakılmaksızın
        /// her alanın saat dağıtımını / atamasını değiştirebilir. Okul müdürü ve müdür yardımcısı
        /// alır; alan şefi ALMAZ.</para>
        ///
        /// <para><b>Ad neden <c>department:</c> ile başlamıyor:</b> <c>DepartmentHead</c> rolü
        /// <c>department:*</c> wildcard'ını taşır. İzin <c>department:distribution:all</c> olarak
        /// adlandırılsaydı wildcard onu da kapsar, muafiyet alan şefine de geçer ve kapsam
        /// kontrolü sessizce hiç çalışmazdı. <c>institution:</c> öneki muafiyeti kurum geneli
        /// yetkiye bağlar: <c>InstitutionManager</c> zaten <c>institution:*</c> ile alır,
        /// <c>InstitutionStaff</c>'a açıkça verilir, <c>DepartmentHead</c>'e hiçbir yoldan geçmez.</para>
        /// </summary>
        public const string AllBranches = "institution:distribution:all-branches";

        /// <summary>
        /// Kurum geneli koordinasyon yapılandırmasını <b>değiştirme</b> yetkisi (#130).
        ///
        /// <para>Kapsadığı ayarlar (<c>CoordinationConfig</c>) alan bazlı değil <b>kurum
        /// düzeyi</b>dir ve mevzuat türevidir: mesafe-saat eşleme tablosu
        /// (<c>DistanceHourRules</c>), büyükşehir sınırı (<c>IsMetropolitan</c>) ve öğretmen
        /// başına azami haftalık ek ders saati (<c>MaxWeeklyExtraHours</c>).</para>
        ///
        /// <para><b>Neden ayrı izin:</b> #126'nın alan kapsamı kontrolü bu uca uygulanamaz —
        /// yapılandırmanın alanı yoktur. Yalnız <see cref="Permissions.DepartmentHead.Distribution"/>
        /// istenseydi alan şefi, doğrudan yazamadığı diğer alanları kurum geneli parametreyi
        /// değiştirerek <b>dolaylı</b> etkilerdi: <c>MaxWeeklyExtraHours</c> düşünce o alanların
        /// mevcut atamaları limit üstüne çıkar, mesafe kuralları değişince tüm alanların
        /// <c>MaxCoordinationHours</c> tavanları ve #116 dağıtım önerileri kayar.</para>
        ///
        /// <para><b>Muafiyet izni (<see cref="AllBranches"/>) neden kullanılmadı:</b> o izin
        /// "tüm <i>alanlara</i> yazabilir" demektir; kurum geneli yapılandırma ise alan
        /// kavramıyla hiç ilgili değildir. Muafiyeti buraya uydurmak anlamını bulanıklaştırırdı.</para>
        ///
        /// <para><b>Wildcard:</b> <c>institution:</c> öneki zorunludur.
        /// <c>DepartmentHead</c> <c>department:*</c> taşır — izin o önekte olsaydı alan şefine
        /// wildcard yoluyla geçer ve kısıt hiç çalışmazdı. <c>InstitutionManager</c>
        /// <c>institution:*</c> ile alır, <c>DeputyDirector</c>'a açıkça verilir;
        /// <c>DepartmentHead</c> ve <c>InstitutionStaff</c> hiçbir yoldan almaz.</para>
        ///
        /// <para><b>Okuma kısıtlanmadı:</b> <c>GET /api/coordination/teachers/config</c>
        /// <see cref="Permissions.DepartmentHead.Distribution"/> ile açık kalır — alan şefi
        /// yapılandırmayı görür, değiştiremez (#126'nın "okuma açık, yazma kapalı" kararı).</para>
        /// </summary>
        public const string CoordinationConfigManage = "institution:coordination-config:manage";

        /// <summary>
        /// <b>Okulda staj</b> yapan öğrencinin dönem notunu girme (#171).
        ///
        /// <para>İşletmede staj yapan öğrencinin notunu işletme girer
        /// (<see cref="Company.EnterGrade"/>, kapsam <c>business_id</c> claim'i). Okulda staj
        /// yapan öğrencinin (#159, işverensiz yerleştirme) işvereni yoktur; o izin ve o kapsam
        /// kullanılamaz — okuldaki yetkilinin <c>business_id</c> claim'i yoktur. Bu izin olmadan
        /// öğrencinin notu <b>hiç girilemiyordu</b>: yerleştirme işletme kapsamlı görünüme
        /// girmediği için not giriş listesinde de görünmüyordu.</para>
        ///
        /// <para><b>Önek neden <c>institution:</c>:</b> öğrenci okulda staj yaptığında kurum,
        /// işverenin yerine geçer — bu bir alan/bölüm işi değil, <b>kurumun</b> işidir. Sahibin
        /// kararı: <i>"Resmî kuruma bağlı izinler kurumsal olmalı."</i></para>
        ///
        /// <para><b>Kimde:</b> <c>InstitutionManager</c> (<c>institution:*</c> ile),
        /// <c>DeputyDirector</c> ve <c>DepartmentHead</c> (<b>açık satırla</b> —
        /// <c>institution:*</c> yalnız müdürdedir). Açık satırlar
        /// <see cref="RolePermissionMap"/>'ten silinirse o iki rol izni kaybeder; kilitleyen
        /// test: <c>tests/MESNET.Security.UnitTests/SchoolTermGradeMappingTests.cs</c>.</para>
        ///
        /// <para><b>Önek kapsamı BELİRLEMEZ</b> (ADR-0001): hangi kurumun öğrencisi sorusunu
        /// <c>institution_id</c> claim'i cevaplar ve o kontrol izinden bağımsız çalışır.</para>
        ///
        /// <para><b>Not: bu izin fiş üretmez.</b> Okulda staj için MEB Form 8 (Dönem Not Fişi)
        /// üretilmez — sahibin ifadesi: <i>"Okulda staj için ayrı form yok, hatta form yok genel
        /// olarak."</i> Kayıt yalnız öğrencinin başarı değerlendirmesi için tutulur.</para>
        /// </summary>
        public const string SchoolGradeEnter = "institution:school-grade:enter";
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

        /// <summary>İşletmenin öğrenci dönem notlarını girmesi/göndermesi (Dönem Not Fişi kaynağı).</summary>
        public const string EnterGrade = "company:grade:enter";
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

        /// <summary>
        /// Devamsızlık belgesi (sağlık raporu vb.) <b>yükleme</b> (#172).
        ///
        /// <para>Giriş bilinçli olarak GENİŞTİR: işletme yetkilisi, işletme İK, usta öğretici ve
        /// öğrenci de yükleyebilir. Para etkisi girişte değil <b>onay zincirinin sonunda</b>
        /// doğar — yükleyen tarafta <see cref="HealthReportDirect"/> yoksa rapor onaya kadar
        /// devamsızlık türünü değiştirmez, dolayısıyla ücret kesintisini kaldırmaz.</para>
        ///
        /// <para><b>Önek:</b> <c>attendance:</c>. <c>attendance:*</c> wildcard'ı yalnız
        /// <c>InstitutionManager</c>'dadır ve orada olması istenen sonuçtur; işletme rolleri
        /// attendance izinlerini tek tek satırla alır, bu yüzden geniş giriş izni onlara
        /// wildcard'la sızmaz, açıkça verilir.</para>
        /// </summary>
        public const string Upload = "attendance:upload";

        /// <summary>Devamsızlık kaydını doğrulama ve onaylama.</summary>
        public const string Approve = "attendance:approve";

        /// <summary>
        /// Girilen <b>devamsızlık kaydının onay beklemeden</b> geçerli olması (#172).
        ///
        /// <para>Bu izin erişim değil <b>hüküm</b> açar: sahibi olan kullanıcının girdiği kayıt
        /// doğrudan <c>Recorded</c> başlar; olmayan kullanıcının girdiği kayıt <c>Pending</c>
        /// başlar ve koordinatör öğretmen onayına kadar ücret kesintisi doğurmaz.</para>
        ///
        /// <para><b>Neyi düzeltir:</b> ayrım daha önce <c>MarkAttendanceHandler</c> içinde
        /// <b>rol adına</b> bakılarak yapılıyordu (<c>IsInRole(CompanyManager)</c> ||
        /// <c>IsInRole(MasterTrainer)</c>) ve CLAUDE.md'de bilinen teknik borç olarak yazılıydı.
        /// Rol adı listesi yeni bir işletme rolü eklendiğinde (ör. <c>CompanyHR</c>) sessizce
        /// eksik kalır ve o rolün girdiği kayıt okul girmiş gibi doğrudan hüküm doğururdu.</para>
        ///
        /// <para><b>Önek neden <c>attendance:</c>:</b> izin okul tarafında olmalı, işletme
        /// tarafında olmamalı. <c>company:</c> KULLANILAMAZ — <c>CompanyManager</c> ve
        /// <c>CompanyHR</c> o öneki taşır. <c>department:</c> de KULLANILAMAZ —
        /// <c>DepartmentHead</c> ve <c>DeputyDirector</c> <c>department:*</c> taşır, izin alan
        /// şefine de geçerdi. <c>attendance:*</c> yalnız <c>InstitutionManager</c>'dadır ve
        /// müdürün bu izne sahip olması istenen sonuçtur. Kilitleyen test:
        /// <c>tests/MESNET.Security.UnitTests/AttendanceDirectEntryMappingTests.cs</c>.</para>
        /// </summary>
        public const string DirectEntry = "attendance:direct-entry";

        /// <summary>
        /// Girilen <b>sağlık raporunun onay beklemeden</b> geçerli olması (#172).
        ///
        /// <para>Sahibin kuralı: "Koordinatör öğretmen, müdür yardımcısı ya da müdür doğrudan
        /// öğrenci sağlık raporunu girebilir, bunda onaya gerek yoktur." Diğer herkesin —
        /// işletme yetkilisi, işletme İK, usta öğretici, öğrenci, veli — girdiği rapor
        /// koordinatör öğretmen onaylayana kadar hüküm doğurmaz.</para>
        ///
        /// <para><b><see cref="DirectEntry"/>'den neden ayrı:</b> iki kayıt aynı kişilerde
        /// bitmiyor. Devamsızlık girişini <c>InstitutionStaff</c> da doğrudan yapar (bugünkü
        /// davranış, #129: yürütür); sağlık raporunda sahibin saydığı taraf yalnız üç roldür.
        /// Tek izne indirmek ya kurum personeline rapor onayı yetkisi verirdi ya da bugünkü
        /// devamsızlık akışını bozardı.</para>
        ///
        /// <para><b>Önek:</b> <see cref="DirectEntry"/> ile aynı gerekçe — <c>attendance:</c>.</para>
        /// </summary>
        public const string HealthReportDirect = "attendance:health-report:direct";

        /// <summary>
        /// MESEM ücretli izin başvurusu açma (#177) — <b>öğrenci</b>.
        ///
        /// <para>Başvuru hüküm doğurmaz: devamsızlık kaydı açılmaz, ücrete etki etmez. Kimin
        /// adına başvurulduğu izinle değil <b>kapsamla</b> belirlenir — <c>StudentId</c> token'ın
        /// <c>student_id</c> claim'inden alınır, istekten ALINMAZ. Veli rolü (#174) geldiğinde
        /// aynı uca eklenecektir.</para>
        /// </summary>
        public const string LeaveRequest = "attendance:leave:request";

        /// <summary>
        /// Ücretli izin başvurusunun <b>işletme</b> onayı (#177) — zincirin 1. adımı.
        ///
        /// <para><b>Bu izin tek başına adımı vermez.</b> <c>InstitutionManager</c>
        /// <c>attendance:*</c> wildcard'ını taşır; hangi önekte tanımlanırsa tanımlansın izin
        /// okul müdürüne de gider (<c>platform:</c> dışında serbest önek yoktur). Adımı işletmeye
        /// bağlayan şey <b>kapsam</b>tır: token'daki <c>business_id</c> claim'i başvurunun
        /// işletmesiyle eşleşmek zorundadır ve okul rollerinde o claim yoktur. ADR-0001:
        /// permission erişimi açar, KAPSAMI belirlemez.</para>
        /// </summary>
        public const string LeaveBusinessApprove = "attendance:leave:business-approve";

        /// <summary>
        /// Ücretli izin başvurusunun <b>okul</b> onayı (#177) — zincirin 2. adımı; izin bu adımla
        /// resmîleşir ve devamsızlık kayıtları doğar.
        ///
        /// <para>Sahibin kararı: onay <c>DeputyDirector</c> ve <c>InstitutionManager</c>'dadır.
        /// Koordinatör öğretmen zincirde adım TUTMAZ, yalnız bildirim alır.</para>
        ///
        /// <para><b>Bireysel atanamaz</b> (<c>AssignablePermissionScope.NeverDirectlyAssignable</c>):
        /// işletme rollerinin atanabilir domain listesinde <c>attendance:</c> vardır; sabit liste
        /// olmasaydı bir işletme kullanıcısına okul adımı bireysel atanabilir ve iki taraflı onay
        /// tek tarafa çökerdi. "Aynı kullanıcı iki adımı yapamaz" kuralı bunu tek başına
        /// kapatmaz — ikinci bir işletme kullanıcısı okul adımını yapardı.</para>
        /// </summary>
        public const string LeaveApprove = "attendance:leave:approve";

        /// <summary>Devamsızlık kaydını silme (son 7 gün, müdür/müdür yardımcısı).</summary>
        public const string Delete = "attendance:delete";
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

        /// <summary>
        /// Asgari ücret ve hesaplama parametrelerini GÖRÜNTÜLEME. Yazma bu izinle YAPILMAZ —
        /// parametreler ulusal mevzuattır ve <see cref="Platform.ParameterManage"/> ister (#147).
        /// </summary>
        public const string ParameterView = "salary:parameter:view";
    }

    /// <summary>
    /// <b>Ulusal (platform) düzeyi izinler — kurum sınırının ÜSTÜNDE (#147).</b>
    ///
    /// <para><b>Önek neden <c>platform:</c>:</b> <see cref="RolePermissionMap"/>'te
    /// <c>InstitutionManager</c> hem <c>"institution:*"</c> hem <c>"salary:*"</c> wildcard'ını
    /// tutuyor. Bu izin <c>salary:national:manage</c> ya da <c>institution:...</c> diye
    /// adlandırılsaydı her okul müdürüne SESSİZCE geçer ve kontrol hiç çalışmazdı — #126'daki
    /// muafiyet-öneki tuzağının birebir tekrarı. <c>platform:</c> öneki hiçbir okul rolünde
    /// yoktur; kilitleyen test:
    /// <c>tests/MESNET.Security.UnitTests/PlatformScopeMappingTests.cs</c>.</para>
    ///
    /// <para>Bu izinler <see cref="AssignablePermissionScope.NeverDirectlyAssignable"/>
    /// listesindedir: bireysel (direct) ASLA atanamaz, yalnız rol üzerinden gelir.</para>
    /// </summary>
    public static class Platform
    {
        /// <summary>
        /// Ulusal hesaplama parametrelerini (asgari ücret, 3308 oranları) güncelleme.
        /// Gerçek işletimde Bakanlık düzeyi bir aktörün işidir; bugün
        /// <c>SystemAdmin</c> rolündedir.
        /// </summary>
        public const string ParameterManage = "platform:parameter:manage";

        /// <summary>
        /// <b>Kurum sınırının üstünde çalışma</b> — yeni okul açmak, herhangi bir okulun kaydını
        /// okumak/yazmak, bir kullanıcıyı herhangi bir okula bağlamak (ADR-0003 adım 6).
        ///
        /// <para>Bu izin olmadan her aktör <b>yalnız kendi okulunda</b> çalışır. Kapsam kararı
        /// izinden değil, aktörün kurum claim'i ile hedef kurumun karşılaştırılmasından çıkar —
        /// <see cref="InstitutionScopePolicy"/>.</para>
        ///
        /// <para><b>İkinci okulun ilk kullanıcısı bu izinle açılır.</b> Ölçüldü: izin yokken
        /// <c>CreateUser</c> herhangi bir <c>InstitutionId</c>'yi kabul ediyordu ve A okulunun
        /// müdürü B okuluna kullanıcı yaratabiliyordu — kilitli kapının yanındaki açık pencere.
        /// O pencere kapanınca ikinci okulu açmak için <b>bilinçli</b> bir yol gerekti.</para>
        /// </summary>
        public const string TenantManage = "platform:tenant:manage";
    }

    /// <summary>
    /// Denetim izi (C parçası).
    /// </summary>
    /// <remarks>
    /// <para><b>Neden ayrı bir önek, neden <c>institution:</c> DEĞİL:</b> <c>institution:</c>
    /// önekli bir izin <c>InstitutionManager</c>'ın <c>institution:*</c> wildcard'ı üzerinden
    /// her okul müdürüne sessizce geçerdi (ADR-0002 önek tuzağı). Okul müdürünün kendi
    /// okulunun izini görmesi istenen bir şeydir; ama kararın wildcard'ın yan etkisiyle değil
    /// AÇIKÇA verilmesi gerekir. Yeni ve çakışmasız önek bunu sağlar.</para>
    ///
    /// <para><b>"Kendi işlemlerim" için izin YOKTUR</b> ve bu bilinçlidir: kullanıcının kendi
    /// geçmişini görmesi bir yetki sorusu değildir. Kapsam <c>ActorId == aktör</c> ile
    /// sunucuda daraltılır.</para>
    /// </remarks>
    public static class Audit
    {
        /// <summary>Kendi kurum ağacının (yol öneki) denetim izini okuma.</summary>
        public const string ViewInstitution = "audit:view:institution";
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

        /// <summary>Haftalık ziyaret ataması oluşturma ve yönetimi.</summary>
        public const string WeeklyVisit = "department:weekly-visit:manage";
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
