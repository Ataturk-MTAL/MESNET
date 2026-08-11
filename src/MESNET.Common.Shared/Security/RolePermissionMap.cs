namespace MESNET.Common.Shared.Security;

/// <summary>
/// Rol → Permission eşleştirmesi.
/// Wildcard desteği: "student:*" → student: ile başlayan tüm izinleri kapsar.
/// Kaynak: src/Docs/docs/actors/permissions.md
/// </summary>
public static class RolePermissionMap
{
    private static readonly Dictionary<string, IReadOnlyList<string>> Mappings = new()
    {
        [MesnetRoles.InstitutionManager] =
        [
            "institution:*",
            "student:*",
            "internship:*",
            "attendance:*",
            "salary:*",
            "document:*",
            "communication:*",
            "user:*",
            "coordinator:*",
            "department:*",
            "company:*",
            // Kurum geneli alan muafiyeti (#126) — "institution:*" zaten kapsar,
            // güvenlik kararı olduğu için açıkça yazılır.
            Permissions.Institution.AllBranches,
            // Kurum geneli koordinasyon yapılandırması (#130) — "institution:*" zaten kapsar,
            // güvenlik kararı olduğu için açıkça yazılır.
            Permissions.Institution.CoordinationConfigManage,
            // Kaydın onay beklemeden hüküm doğurması (#172) — "attendance:*" zaten kapsar,
            // para etkisi olan bir karar olduğu için açıkça yazılır.
            Permissions.Attendance.DirectEntry,
            Permissions.Attendance.HealthReportDirect,
            Permissions.Attendance.Upload,
            // Ücretli izin okul onayı (#177) — "attendance:*" zaten kapsar, para etkisi olduğu
            // için açıkça yazılır. İşletme adımı (LeaveBusinessApprove) da wildcard'la buraya
            // gelir; onu engelleyen izin değil business_id KAPSAMIDIR — müdürde o claim yoktur.
            Permissions.Attendance.LeaveApprove,
            // Okulda staj dönem notu (#171) — "institution:*" zaten kapsar, açıkça yazılır.
            Permissions.Institution.SchoolGradeEnter
        ],
        // Müdür yardımcısı (#129). Kaynak: actors.md → "Müdür Yardımcısı" —
        // staj işlemleri koordinasyonu, evrak takibi ve onayı, öğretmen görevlendirmeleri,
        // dekont ve maaş süreçleri yönetimi.
        // Bu demet #129 öncesinde InstitutionStaff'ta duruyordu; gerçekte müdür yardımcısının
        // demetiydi (yorumları da öyle diyordu). Ayrı role taşındı, InstitutionStaff daraltıldı.
        [MesnetRoles.DeputyDirector] =
        [
            "user:*",                              // davet onayı, kullanıcı/rol yönetimi
            "department:*",                        // öğretmen görevlendirme + işletme dağıtımı
            // Müdür yardımcısı tüm alanların dağıtımını yönetebilir (#126).
            // DepartmentHead bu izni ALMAZ — alan kapsamı kontrolü ona uygulanır.
            // InstitutionStaff da ALMAZ (#129) — koordinasyon dağıtımı onun görevi değil.
            Permissions.Institution.AllBranches,
            // Kurum geneli koordinasyon yapılandırması (#130): mesafe-saat kuralları,
            // büyükşehir sınırı ve azami haftalık ek ders saati. Alan bazlı değil kurum
            // düzeyi bir ayardır; DepartmentHead ve InstitutionStaff ALMAZ.
            Permissions.Institution.CoordinationConfigManage,
            Permissions.Institution.View,
            Permissions.Student.View,
            Permissions.Student.Manage,
            Permissions.Internship.View,       // staj listesi görüntüleme
            Permissions.Internship.Manage,     // fesih talebi başlatma, genel yönetim
            Permissions.Internship.Approve,    // fesih onay zinciri (kendi adımı)
            Permissions.Internship.Contract,   // sözleşme yönetimi
            Permissions.Salary.View,
            Permissions.Salary.Calculate,
            Permissions.Salary.Approve,        // dekont onay zinciri
            // Asgari ücret ve 3308 oranları ULUSAL mevzuattır; müdür yardımcısı GÖRÜR,
            // değiştiremez (#147). Yazma izni Platform.ParameterManage'dir ve yalnız
            // SystemAdmin'dedir — bu satır bilinçli olarak "view".
            Permissions.Salary.ParameterView,
            Permissions.Attendance.View,
            Permissions.Attendance.Manage,
            Permissions.Attendance.Report,
            Permissions.Attendance.Approve,
            Permissions.Attendance.Upload,          // sağlık raporu girişi (#172)
            // Müdür yardımcısının girdiği kayıt onay beklemez (#172) — sahibin kuralı:
            // "koordinatör öğretmen, müdür yardımcısı ya da müdür doğrudan girebilir".
            Permissions.Attendance.DirectEntry,
            Permissions.Attendance.HealthReportDirect,
            // Ücretli izin zincirinin 2. adımı (#177) — sahibin kararı: "müdür yardımcısı ve
            // müdür yeterli". Koordinatör öğretmen ALMAZ; ona yalnız bildirim gider.
            Permissions.Attendance.LeaveApprove,
            // Okulda staj dönem notu (#171). "institution:*" YALNIZ müdürdedir; müdür
            // yardımcısı bu izni ancak bu satırla alır — satır silinirse izni kaybeder.
            Permissions.Institution.SchoolGradeEnter,
            Permissions.Document.View,
            Permissions.Document.Upload,
            Permissions.Document.Verify,
            Permissions.Document.Track,
            Permissions.Document.Approve,      // evrak onayı
            Permissions.Company.View,
            Permissions.Company.Manage,
            Permissions.Company.Document,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        // Kurum yetkilendirdiği personel (#129). Kaynak: actors.md → "Kurum Yetkilendirdiği
        // Personel" — öğrenci kayıt işlemleri, belge doğrulama, devamsızlık takibi,
        // maaş hesaplamaları. Yürütür ama ONAYLAMAZ ve kullanıcı/koordinasyon yönetmez:
        // "user:*", "department:*", *.Approve ve kapsam muafiyeti DeputyDirector'dedir.
        [MesnetRoles.InstitutionStaff] =
        [
            Permissions.Institution.View,
            Permissions.Student.View,
            Permissions.Student.Manage,        // öğrenci kayıt işlemleri
            Permissions.Internship.View,       // öğrenci/devamsızlık işi staj bağlamında yürür
            Permissions.Salary.View,
            Permissions.Salary.Calculate,      // maaş hesaplamaları (onay müdür yardımcısında)
            Permissions.Attendance.View,
            Permissions.Attendance.Manage,     // devamsızlık takibi
            Permissions.Attendance.Report,
            Permissions.Attendance.Upload,     // sağlık raporu girişi (#172)
            // Devamsızlık girişi onay beklemez — bugünkü davranış korunur (#172). Sağlık
            // raporunda karşılığı olan HealthReportDirect ise YOKTUR: sahibin saydığı taraf
            // koordinatör öğretmen, müdür yardımcısı ve müdürdür. Personelin girdiği rapor
            // onaya düşer — #129'un "yürütür, onaylamaz" ayrımıyla aynı çizgi.
            Permissions.Attendance.DirectEntry,
            Permissions.Document.View,
            Permissions.Document.Upload,
            Permissions.Document.Verify,       // belge doğrulama
            Permissions.Document.Track,
            Permissions.Company.View,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        [MesnetRoles.Teacher] =
        [
            Permissions.Student.View,
            Permissions.Student.Manage,
            Permissions.Internship.View,
            Permissions.Internship.Review,     // başvuru inceleme
            Permissions.Internship.Approve,    // fesih onay zincirinde kendi adımı
            Permissions.Attendance.View,
            Permissions.Attendance.Manage,
            Permissions.Attendance.Report,
            // Sağlık raporu onay zincirinin 1. adımı koordinatör öğretmendedir (#172):
            // işletme/öğrenci/veli girişini o onaylar ya da reddeder.
            Permissions.Attendance.Approve,
            Permissions.Attendance.Upload,
            Permissions.Attendance.DirectEntry,
            Permissions.Attendance.HealthReportDirect,
            Permissions.Salary.View,
            Permissions.Salary.Approve,        // dekont ilk onay (öğretmen adımı)
            Permissions.Coordinator.Schedule,
            Permissions.Coordinator.Visit,
            Permissions.Coordinator.Report,
            Permissions.Coordinator.Communication,
            Permissions.Document.View,
            Permissions.Document.Upload,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        [MesnetRoles.Student] =
        [
            Permissions.Student.ViewOwn,
            Permissions.Student.UpdateOwn,
            Permissions.Internship.ViewOwn,
            Permissions.Internship.Apply,
            Permissions.Attendance.ViewOwn,
            // Öğrenci kendi sağlık raporunu yükleyebilir (#172). Yükleme hüküm doğurmaz:
            // DirectEntry / HealthReportDirect bu rolde yoktur, rapor onaya düşer.
            Permissions.Attendance.Upload,
            // MESEM ücretli izin başvurusu (#177). Başvuru da hüküm doğurmaz: işletme ve okul
            // onayından geçmeden izin günü açılmaz. Kimin adına başvurulduğu student_id
            // claim'inden gelir — öğrenci başkası adına başvuramaz.
            Permissions.Attendance.LeaveRequest,
            Permissions.Salary.ViewOwn,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage,
            Permissions.Communication.ReportIssue
        ],
        [MesnetRoles.DepartmentHead] =
        [
            "department:*",
            // Okulda staj yapan öğrencinin dönem notu (#171). Alan şefinde "institution:"
            // öneki HİÇ YOKTUR; izin yalnız bu açık satırla gelir — silinirse alan şefi notu
            // giremez. İşletmede staj notunu işletme girer; bu izin yalnız işverensiz
            // yerleştirme (#159) içindir.
            Permissions.Institution.SchoolGradeEnter,
            Permissions.Student.View,
            Permissions.Coordinator.Schedule,
            Permissions.Attendance.View,
            Permissions.Attendance.Report,
            Permissions.Attendance.Delete,
            Permissions.Document.View,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        [MesnetRoles.CompanyManager] =
        [
            Permissions.Company.View,
            Permissions.Company.Manage,
            Permissions.Company.Document,
            Permissions.Company.Student,
            // Fesih onay adımını yapabilmek için kendi stajlarını GÖRMESİ gerekir (#191).
            // "view" değil "view-own": kapsamı OwnDataScope çözer ve işletme kimliği
            // claim'den okunur — işletme yalnız KENDİ stajlarını görür.
            Permissions.Internship.ViewOwn,
            Permissions.Company.Attendance,
            Permissions.Company.UploadReceipt,
            Permissions.Company.MasterTrainer,
            Permissions.Company.RequestStudent,
            Permissions.Company.EnterGrade,
            Permissions.Attendance.Manage,
            // Sağlık raporu girişi (#172). DirectEntry / HealthReportDirect YOKTUR: işletmenin
            // girdiği kayıt onaya düşer. Ödemeyi yapan taraf kendi kararıyla kesintiyi
            // kaldıramaz — #172'nin kapattığı asıl açık budur.
            Permissions.Attendance.Upload,
            // Ücretli izin zincirinin 1. adımı (#177). Öğrencinin o gün işletmede olmayacağına
            // önce işveren karar verir; okul onayı ondan sonra gelir.
            Permissions.Attendance.LeaveBusinessApprove,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        // Usta öğretici (#129) — işletme içinde DAR yetkili. CompanyManager'ın geniş demetini
        // ALMAZ: öğrenci talebi, dekont yükleme ve işletme belge yönetimi onda kalır.
        [MesnetRoles.MasterTrainer] =
        [
            Permissions.Company.Attendance,    // devam takibi
            Permissions.Company.EnterGrade,    // dönem notu girişi
            Permissions.Student.View,          // kendi öğrencileri
            // Devamsızlık girişi ucu "attendance:manage" ister; bu satır olmadan usta öğretici
            // POST /api/attendance'a hiç ulaşamıyordu — oysa MarkAttendanceHandler onu giriş
            // yapan işletme aktörü sayıyor ve kaydını Pending'e düşürüyordu (#129/#172).
            Permissions.Attendance.Manage,
            Permissions.Attendance.Upload,     // sağlık raporu tarayıp girme
            // Ücretli izin işletme onayı (#177) — usta öğretici öğrencinin günlük devamını
            // bilen taraftır; her işletmede ayrı bir yetkili ya da İK bulunmaz.
            Permissions.Attendance.LeaveBusinessApprove,
            "communication:*"
        ],
        // İşletme insan kaynakları (#172). Sahibin kuralı: sağlık raporunu "işletme müdürü,
        // işletmenin insan kaynakları ya da usta öğretici tarayıp girebilir".
        // CompanyManager'ın geniş demetini ALMAZ: öğrenci talebi, dekont yükleme, işletme belge
        // yönetimi ve dönem notu girişi yöneticide kalır.
        [MesnetRoles.CompanyHR] =
        [
            Permissions.Company.View,
            Permissions.Company.Attendance,    // işletme devam çizelgesi
            Permissions.Attendance.Manage,     // devamsızlık girişi (kaydı Pending başlar)
            Permissions.Attendance.Upload,     // sağlık raporu tarayıp girme
            Permissions.Attendance.LeaveBusinessApprove, // ücretli izin işletme onayı (#177)
            Permissions.Student.View,          // işletmedeki öğrenciler
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        // Veli (#174). Demet DAR: veli veri GİRER ama hiçbir girişi hüküm doğurmaz ve hiçbir
        // şeyi onaylamaz — fesih zincirindeki kendi adımı hariç. Kapsamı izinle değil
        // UserAccount.LinkedStudentIds KAYDIYLA sınırlıdır (ADR-0001): aşağıdaki izinler tüm
        // velilerde aynıdır, onları ayıran tek şey hangi öğrenciye bağlı oldukları.
        [MesnetRoles.Parent] =
        [
            Permissions.Student.ViewOwn,        // öğrencisinin profili
            Permissions.Internship.ViewOwn,     // öğrencisinin staj durumu
            Permissions.Attendance.ViewOwn,     // öğrencisinin devamsızlığı
            // Sağlık raporu girişi (#172) — onaya düşer. DirectEntry / HealthReportDirect
            // bu rolde YOKTUR: veli, öğrencisinin ücret kesintisini tek taraflı kaldıramaz.
            Permissions.Attendance.Upload,
            // MESEM ücretli izin başvurusu (#177) — öğrenci adına açar, yine iki taraflı
            // onaydan geçer. #177'de "veli rolü gelince aynı uca eklenir" diye yazılmıştı.
            Permissions.Attendance.LeaveRequest,
            Permissions.Salary.ViewOwn,         // öğrencisinin ücret bilgisi
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage,
            Permissions.Communication.ReportIssue
        ],
        // Sistem yöneticisi (#147) — ULUSAL parametre girişi. Kurum domainlerinden HİÇBİRİ
        // yoktur: bu rol kurum verisi görmez, yalnız mevzuat sayılarını yazar. Tersi de
        // geçerli — okul rollerinin hiçbirinde "platform:" öneki yoktur.
        [MesnetRoles.SystemAdmin] =
        [
            Permissions.Platform.ParameterManage,
            // Kurum sınırının üstünde çalışma (ADR-0003 adım 6): yeni okul açmak ve ilk
            // kullanıcısını o okula bağlamak. Okul rollerinin hiçbirinde yoktur — okul müdürü
            // kendi okulunda kalır. Bu izin olmasaydı ikinci okulun ilk kullanıcısı HİÇBİR
            // yoldan açılamazdı: kapsamsız aktör bağ yazamaz, A'nın müdürü B'ye yazamaz.
            Permissions.Platform.TenantManage,
            // Yeni okulun İLK kullanıcısını (müdürünü) açabilmesi için kullanıcı yönetimi
            // gerekir — okulu açıp içine kimseyi koyamamak işe yaramaz. Kurum VERİSİ yine
            // kapalıdır: institution:view/manage VERİLMEZ, o yüzden bu rol okul listesini bile
            // görmez. Açtığı okula bağlama yetkisi izinden değil kapsam muafiyetinden gelir
            // (UserInstitutionScopePolicy).
            Permissions.UserManagement.Create,
            Permissions.UserManagement.RolesManage,
            // Yazdığı değerin yürürlük geçmişini görebilmesi için okuma izni de gerekir;
            // "salary:*" verilMEZ — dekont, hesaplama ve öğrenci verisi bu role kapalıdır.
            Permissions.Salary.ParameterView
        ]
    };

    private static readonly Lazy<IReadOnlyList<string>> AllPermissions = new(() => Permissions.GetAll());

    /// <summary>
    /// Verilen roller için tüm permission'ları döndürür.
    /// Wildcard ifadeler ("student:*") genişletilir.
    /// </summary>
    public static IReadOnlyList<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            if (!Mappings.TryGetValue(role, out var rolePermissions))
                continue;

            foreach (var permission in rolePermissions)
            {
                if (permission.EndsWith(":*"))
                {
                    var prefix = permission[..^1]; // "student:*" → "student:"
                    foreach (var expanded in AllPermissions.Value)
                    {
                        if (expanded.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            permissions.Add(expanded);
                    }
                }
                else
                {
                    permissions.Add(permission);
                }
            }
        }

        return permissions.ToList();
    }

    /// <summary>
    /// Belirli bir rolün ham permission listesini döndürür (wildcard genişletilmez).
    /// UI'da rol detayı göstermek için kullanılır.
    /// </summary>
    public static IReadOnlyList<string> GetRawPermissionsForRole(string role)
    {
        return Mappings.TryGetValue(role, out var permissions)
            ? permissions
            : [];
    }

    /// <summary>
    /// Belirli bir permission'ın wildcard eşleşmesiyle kontrol eder.
    /// Kullanıcıda "student:*" varsa, "student:view" için true döner.
    /// </summary>
    public static bool MatchesPermission(string userPermission, string requiredPermission)
    {
        if (string.Equals(userPermission, requiredPermission, StringComparison.OrdinalIgnoreCase))
            return true;

        if (userPermission.EndsWith(":*"))
        {
            var prefix = userPermission[..^1];
            return requiredPermission.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
