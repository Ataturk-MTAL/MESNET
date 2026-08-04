namespace MESNET.Seeder.Seeders;

public static class EnrollmentSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        if (!ctx.Has("Institution") || !ctx.Has("AcademicPeriod")) return;
        var institutionId = ctx.Get("Institution");
        var academicPeriodId = ctx.Get("AcademicPeriod");

        await SeedTeachers(api, ctx, institutionId);
        await SeedStudents(api, ctx, institutionId, academicPeriodId);
        await SeedPlacements(api, ctx, institutionId, academicPeriodId);
    }

    private static async Task SeedTeachers(MesnetApiClient api, SeedContext ctx, Guid institutionId)
    {
        Console.WriteLine();
        Console.WriteLine("── Öğretmenler ────────────────────");

        // Mevcut öğretmenleri yükle — GetAsync → envelope.Data = PagedResult { items: [...] }
        var existing = await api.GetAllPagedAsync($"/api/teachers?institutionId={institutionId}");
        var existingByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in existing)
        {
            var name = item.GetProperty("fullName").GetString() ?? "";
            var id = item.GetProperty("id").GetGuid();
            existingByName[name] = id;
        }

        // 6 öğretmen: her alandan 2
        var teachers = new[]
        {
            ("Teacher1", "51000000-0000-0000-0000-000000000001", "Ayşe Çelik",    "EET"),  // Elektrik-Elektronik
            ("Teacher2", "51000000-0000-0000-0000-000000000002", "Mustafa Yılmaz", "EET"),  // Elektrik-Elektronik
            ("Teacher3", "51000000-0000-0000-0000-000000000003", "Hasan Kara",     "BT"),   // Bilişim
            ("Teacher4", "51000000-0000-0000-0000-000000000004", "Fatma Özdemir",  "BT"),   // Bilişim
            ("Teacher5", "51000000-0000-0000-0000-000000000005", "Ali Demir",      "MTT"),  // Makine
            ("Teacher6", "51000000-0000-0000-0000-000000000006", "Zeynep Arslan",  "MTT"),  // Makine
        };

        foreach (var (key, kcId, name, branchCode) in teachers)
        {
            if (existingByName.TryGetValue(name, out var existingId))
            {
                ctx.Set(key, existingId);
                Console.WriteLine($"  → Öğretmen \"{name}\" mevcut, yüklendi");
                continue;
            }

            var data = await api.PostAsync("/api/teachers", new
            {
                institutionId,
                keycloakUserId = Guid.Parse(kcId),
                fullName = name,
                branchCode
            });
            if (data is not null)
            {
                ctx.Set(key, data.Value.GetProperty("id").GetGuid());
                Console.WriteLine($"  ✓ Öğretmen \"{name}\" ({branchCode}) oluşturuldu");
            }
        }
    }

    private static async Task SeedStudents(MesnetApiClient api, SeedContext ctx, Guid institutionId, Guid academicPeriodId)
    {
        Console.WriteLine();
        Console.WriteLine("── Öğrenciler ─────────────────────");

        // Mevcut öğrencilerin TAMAMI — tek sayfa yetmez, sunucu pageSize'ı 100'e kırpar ve
        // görülmeyen her öğrenci "yok" sayılıp yeniden yaratılırdı (bkz. GetAllPagedAsync).
        var existingStudents = await api.GetAllPagedAsync($"/api/students?institutionId={institutionId}");
        var existingByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var missingNumberByName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in existingStudents)
        {
            var name = item.GetProperty("fullName").GetString() ?? "";
            var id = item.GetProperty("id").GetGuid();
            existingByName[name] = id;

            // Numara alanı sonradan eklendi (#99) — eski kayıtlar boş; geriye dönük doldurulur.
            if (!item.TryGetProperty("studentNumber", out var numberEl)
                || string.IsNullOrWhiteSpace(numberEl.GetString()))
            {
                missingNumberByName.Add(name);
            }
        }

        // 120 öğrenci: 3 alan × 40 öğrenci (12/A: 20, 12/B: 20)
        // Norm Kadro Yönetmeliği Madde 22: 12. sınıf 20 öğrenci → 2 grup
        var students = GenerateStudents();
        var createdCount = 0;
        var backfilledCount = 0;

        foreach (var s in students)
        {
            if (existingByName.TryGetValue(s.Name, out var existingId))
            {
                ctx.Set(s.Key, existingId);

                if (missingNumberByName.Contains(s.Name))
                {
                    await api.PatchAsync($"/api/students/{existingId}", new { studentNumber = s.StudentNumber });
                    backfilledCount++;
                }

                continue;
            }

            var data = await api.PostAsync("/api/students", new
            {
                institutionId,
                academicPeriodId,
                keycloakUserId = Guid.Parse(s.KcId),
                fullName = s.Name,
                branchCode = s.BranchCode,
                branchName = s.BranchName,
                classYear = s.ClassYear,
                section = s.Section,
                educationType = s.EducationType,
                studentNumber = s.StudentNumber
            });
            if (data is not null)
            {
                ctx.Set(s.Key, data.Value.GetProperty("id").GetGuid());
                createdCount++;
                if (createdCount % 20 == 0)
                    Console.WriteLine($"  ✓ {createdCount} öğrenci oluşturuldu...");
            }
        }

        if (createdCount > 0)
            Console.WriteLine($"  ✓ Toplam {createdCount} yeni öğrenci oluşturuldu (3 alan × 40 = 120 hedef)");
        else
            Console.WriteLine("  → Tüm öğrenciler zaten mevcut");

        if (backfilledCount > 0)
        {
            Console.WriteLine($"  ✓ {backfilledCount} öğrenciye numara geriye dönük yazıldı");

            // PATCH yalnız Enrollment entity'sini günceller; diğer modüllerin denormalize
            // read-model'leri (ör. Reporting.StudentPlacementReportView) StudentRegistered'ı
            // yeniden yayınlamadan tazelenmez (#99).
            await api.PostAsync("/api/students/resync-projections");
            Console.WriteLine("  ✓ Öğrenci read-model'leri yeniden yayınlandı");
        }
    }

    private static List<StudentData> GenerateStudents()
    {
        var students = new List<StudentData>();
        var kcIdCounter = 1;

        // EET — Elektrik-Elektronik Teknolojisi: 40 öğrenci (12/A: 20, 12/B: 20)
        var eetNames = new[]
        {
            "Ahmet Yıldırım", "Mehmet Kaya", "Emre Arslan", "Burak Şahin", "Oğuz Demirtaş",
            "Kerem Aktaş", "Serkan Çetin", "Ufuk Yalçın", "Barış Koç", "Tolga Aydın",
            "Murat Erdoğan", "Onur Polat", "Yusuf Güneş", "Halil Kılıç", "İbrahim Öztürk",
            "Ramazan Çakır", "Selim Doğan", "Taner Ünal", "Volkan Aslan", "Erdem Kaplan",
            "Gökhan Taş", "Hakan Yılmaz", "İsmail Demir", "Kadir Bayrak", "Levent Çelik",
            "Mert Aksoy", "Necati Kurt", "Osman Korkmaz", "Recep Turan", "Sinan Avcı",
            "Tarık Başaran", "Umut Güler", "Veli Şen", "Yasin Karaca", "Zafer Yıldız",
            "Alper Bozkurt", "Bülent Saygılı", "Cenk Türkmen", "Doğan Pınar", "Erol Çınar"
        };
        AddBranchStudents(students, ref kcIdCounter, branchOrdinal: 1,
            "EET", "Elektrik-Elektronik Teknolojisi", eetNames);

        // BT — Bilişim Teknolojileri: 40 öğrenci (12/A: 20, 12/B: 20)
        var btNames = new[]
        {
            "Elif Demir", "Can Yıldız", "Ceren Aksoy", "Deniz Koç", "Selin Aydın",
            "Berk Özkan", "Cem Kılıç", "Duru Şahin", "Ege Arslan", "Furkan Çetin",
            "Gizem Polat", "Hande Yalçın", "İrem Aktaş", "Kaan Erdoğan", "Lale Güneş",
            "Melih Öztürk", "Nisa Çakır", "Ozan Doğan", "Pelin Ünal", "Rüzgar Aslan",
            "Sude Kaplan", "Tuna Taş", "Umay Yılmaz", "Vahit Demir", "Yağız Bayrak",
            "Zehra Çelik", "Aslı Aksoy", "Batuhan Kurt", "Cansu Korkmaz", "Doruk Turan",
            "Ela Avcı", "Ferit Başaran", "Gül Güler", "Harun Şen", "İlayda Karaca",
            "Koray Yıldız", "Leman Bozkurt", "Mete Saygılı", "Naz Türkmen", "Oktay Pınar"
        };
        AddBranchStudents(students, ref kcIdCounter, branchOrdinal: 2,
            "BT", "Bilişim Teknolojileri", btNames);

        // MTT — Makine ve Tasarım Teknolojisi: 40 öğrenci (12/A: 20, 12/B: 20)
        var mttNames = new[]
        {
            "Yiğit Kara", "Arda Özdemir", "Berkay Arslan", "Cihan Yılmaz", "Eren Çelik",
            "Görkem Şahin", "Haydar Kaya", "İlker Demir", "Kağan Aydın", "Mücahit Koç",
            "Nihat Polat", "Orhan Güneş", "Poyraz Kılıç", "Rıdvan Öztürk", "Savaş Çakır",
            "Turgut Doğan", "Ünal Aslan", "Yavuz Kaplan", "Adem Taş", "Bilal Yılmaz",
            "Cengiz Demir", "Devrim Bayrak", "Ertuğrul Çelik", "Ferhat Aksoy", "Gürkay Kurt",
            "Hüseyin Korkmaz", "İhsan Turan", "Kubilay Avcı", "Lokman Başaran", "Mahmut Güler",
            "Nazım Şen", "Orçun Karaca", "Polat Yıldız", "Raşit Bozkurt", "Sedat Saygılı",
            "Taylan Türkmen", "Uğurcan Pınar", "Veysel Çınar", "Yalçın Aktaş", "Zeki Erdoğan"
        };
        AddBranchStudents(students, ref kcIdCounter, branchOrdinal: 3,
            "MTT", "Makine ve Tasarım Teknolojisi", mttNames);

        return students;
    }

    /// <summary>12. sınıf şube mevcudu — Norm Kadro Yönetmeliği md. 22.</summary>
    private const int SectionSize = 20;

    private const int ClassYear = 12;

    /// <summary>
    /// Bir alanın öğrencilerini şubelere bölerek ekler ve okul numarası üretir.
    /// Numara biçimi: {alan}{şube}{şube-içi sıra:00} → EET 12/A 1. öğrenci = "1101",
    /// EET 12/B 1. öğrenci = "1201", BT 12/A 1. öğrenci = "2101". Sınıf/şube ile tutarlı ve tekildir (#99).
    /// </summary>
    private static void AddBranchStudents(
        List<StudentData> students, ref int kcIdCounter,
        int branchOrdinal, string branchCode, string branchName, string[] names)
    {
        for (var i = 0; i < names.Length; i++)
        {
            var sectionIndex = i / SectionSize;               // 0 → A, 1 → B
            var section = ((char)('A' + sectionIndex)).ToString();
            var seqInSection = i % SectionSize + 1;
            var studentNumber = $"{branchOrdinal}{sectionIndex + 1}{seqInSection:D2}";

            students.Add(new StudentData(
                $"{branchCode}_Student{i + 1}",
                $"41000000-0000-0000-0000-{kcIdCounter++:D12}",
                names[i],
                branchCode,
                branchName,
                ClassYear,
                section,
                "Formal",
                studentNumber
            ));
        }
    }

    private static async Task SeedPlacements(MesnetApiClient api, SeedContext ctx, Guid institutionId, Guid academicPeriodId)
    {
        Console.WriteLine();
        Console.WriteLine("── Yerleştirmeler ─────────────────");

        // Eski ctx key alias'ları: Diğer seeder'lar Student1-8 referansı kullanıyor
        // Bunları yeni key'lere eşle
        AliasIfMissing(ctx, "Student1", "EET_Student1");
        AliasIfMissing(ctx, "Student2", "BT_Student1");
        AliasIfMissing(ctx, "Student3", "BT_Student3");
        AliasIfMissing(ctx, "Student4", "BT_Student4");
        AliasIfMissing(ctx, "Student5", "BT_Student5");
        AliasIfMissing(ctx, "Student6", "MTT_Student1");
        AliasIfMissing(ctx, "Student7", "MTT_Student2");
        AliasIfMissing(ctx, "Student8", "MTT_Student3");

        // Yerleştirmelerin TAMAMI tek yerden okunur; hem "bu öğrenci yerleşmiş mi" hem
        // "bu alanda okulda staj var mı" kararı aynı listeden verilir.
        var allPlacements = await api.GetAllPagedAsync("/api/placements");
        var placedStudents = new HashSet<Guid>(
            allPlacements.Select(p => p.GetProperty("studentId").GetGuid()));

        // EET: 30/40 öğrenci yerleştirilir (bazı işletmelere 2-3 öğrenci)
        // BT & MTT: mevcut yerleştirmeler korunur
        var placements = new (string Key, string StudentKey, string BusinessKey, string? TeacherKey)[]
        {
            // ── EET öğrencileri (30 öğrenci → 13 işletme) ──────────────────
            // Business1 (Mezitli Elektrik Mühendislik): 2 öğrenci
            ("Placement1",  "EET_Student1",  "Business1",  "Teacher1"),
            ("Placement2",  "EET_Student2",  "Business1",  "Teacher1"),
            // Business4 (Akdeniz Otomasyon): 2 öğrenci
            ("Placement3",  "EET_Student3",  "Business4",  "Teacher2"),
            ("Placement4",  "EET_Student4",  "Business4",  "Teacher2"),
            // EET_Biz1 (Mezitli Elektrik Sanayi): 4 öğrenci
            ("Placement13", "EET_Student5",  "EET_Biz1",   "Teacher1"),
            ("Placement14", "EET_Student6",  "EET_Biz1",   "Teacher1"),
            ("Placement15", "EET_Student7",  "EET_Biz1",   "Teacher1"),
            ("Placement16", "EET_Student29", "EET_Biz1",   "Teacher2"),
            // EET_Biz2 (Akdeniz Enerji Sistemleri): 3 öğrenci
            ("Placement17", "EET_Student8",  "EET_Biz2",   "Teacher1"),
            ("Placement18", "EET_Student9",  "EET_Biz2",   "Teacher1"),
            ("Placement19", "EET_Student30", "EET_Biz2",   "Teacher2"),
            // EET_Biz3 (Solar Panel Mersin): 2 öğrenci
            ("Placement20", "EET_Student10", "EET_Biz3",   "Teacher2"),
            ("Placement21", "EET_Student11", "EET_Biz3",   "Teacher2"),
            // EET_Biz4 (Endüstriyel Kontrol Sistemleri): 2 öğrenci
            ("Placement22", "EET_Student12", "EET_Biz4",   "Teacher2"),
            ("Placement23", "EET_Student13", "EET_Biz4",   "Teacher2"),
            // EET_Biz5 (Akıllı Ev Teknolojileri): 2 öğrenci
            ("Placement24", "EET_Student14", "EET_Biz5",   "Teacher1"),
            ("Placement25", "EET_Student15", "EET_Biz5",   "Teacher1"),
            // EET_Biz6 (Elektro-Tek Mühendislik): 2 öğrenci
            ("Placement26", "EET_Student16", "EET_Biz6",   "Teacher1"),
            ("Placement27", "EET_Student17", "EET_Biz6",   "Teacher1"),
            // EET_Biz7 (Otomasyon Teknoloji): 3 öğrenci
            ("Placement28", "EET_Student18", "EET_Biz7",   "Teacher2"),
            ("Placement29", "EET_Student19", "EET_Biz7",   "Teacher2"),
            ("Placement30", "EET_Student20", "EET_Biz7",   "Teacher2"),
            // EET_Biz8 (LED Aydınlatma): 2 öğrenci
            ("Placement31", "EET_Student21", "EET_Biz8",   "Teacher2"),
            ("Placement32", "EET_Student22", "EET_Biz8",   "Teacher2"),
            // EET_Biz9 (Mekatronik Çözümler): 2 öğrenci
            ("Placement33", "EET_Student23", "EET_Biz9",   "Teacher1"),
            ("Placement34", "EET_Student24", "EET_Biz9",   "Teacher1"),
            // EET_Biz10 (Asansör Teknik): 2 öğrenci
            ("Placement35", "EET_Student25", "EET_Biz10",  "Teacher1"),
            ("Placement36", "EET_Student26", "EET_Biz10",  "Teacher1"),
            // EET_Biz11 (Medikal Cihaz): 2 öğrenci
            ("Placement37", "EET_Student27", "EET_Biz11",  "Teacher2"),
            ("Placement38", "EET_Student28", "EET_Biz11",  "Teacher2"),

            // ── BT öğrencileri (30 öğrenci → 14 işletme) ──────────────────
            // Business2 (Mersin Bilişim): 2 öğrenci
            ("Placement5",  "BT_Student1",   "Business2",  "Teacher3"),
            ("Placement6",  "BT_Student2",   "Business2",  "Teacher3"),
            // Business5 (Yeni Nesil Teknoloji): 2 öğrenci
            ("Placement7",  "BT_Student3",   "Business5",  "Teacher4"),
            ("Placement8",  "BT_Student4",   "Business5",  "Teacher4"),
            // BT_Biz1 (Datamarin Yazılım): 3 öğrenci
            ("Placement39", "BT_Student5",   "BT_Biz1",    "Teacher3"),
            ("Placement40", "BT_Student6",   "BT_Biz1",    "Teacher3"),
            ("Placement41", "BT_Student7",   "BT_Biz1",    "Teacher3"),
            // BT_Biz2 (Netsis Mersin Bayi): 2 öğrenci
            ("Placement42", "BT_Student8",   "BT_Biz2",    "Teacher3"),
            ("Placement43", "BT_Student9",   "BT_Biz2",    "Teacher3"),
            // BT_Biz3 (Kodlama Akademi): 2 öğrenci
            ("Placement44", "BT_Student10",  "BT_Biz3",    "Teacher4"),
            ("Placement45", "BT_Student11",  "BT_Biz3",    "Teacher4"),
            // BT_Biz4 (Akıllı Çözümler Bilişim): 2 öğrenci
            ("Placement46", "BT_Student12",  "BT_Biz4",    "Teacher4"),
            ("Placement47", "BT_Student13",  "BT_Biz4",    "Teacher4"),
            // BT_Biz5 (Mersin Web Tasarım): 2 öğrenci
            ("Placement48", "BT_Student14",  "BT_Biz5",    "Teacher3"),
            ("Placement49", "BT_Student15",  "BT_Biz5",    "Teacher3"),
            // BT_Biz6 (Bulut Bilgi Sistemleri): 2 öğrenci
            ("Placement50", "BT_Student16",  "BT_Biz6",    "Teacher3"),
            ("Placement51", "BT_Student17",  "BT_Biz6",    "Teacher3"),
            // BT_Biz7 (Siber Güvenlik Mersin): 2 öğrenci
            ("Placement52", "BT_Student18",  "BT_Biz7",    "Teacher4"),
            ("Placement53", "BT_Student19",  "BT_Biz7",    "Teacher4"),
            // BT_Biz8 (ERP Çözümleri): 2 öğrenci
            ("Placement54", "BT_Student20",  "BT_Biz8",    "Teacher4"),
            ("Placement55", "BT_Student21",  "BT_Biz8",    "Teacher4"),
            // BT_Biz9 (Yapay Zeka Laboratuvarı): 2 öğrenci
            ("Placement56", "BT_Student22",  "BT_Biz9",    "Teacher3"),
            ("Placement57", "BT_Student23",  "BT_Biz9",    "Teacher3"),
            // BT_Biz10 (Akdeniz Veri Merkezi): 3 öğrenci
            ("Placement58", "BT_Student24",  "BT_Biz10",   "Teacher4"),
            ("Placement59", "BT_Student25",  "BT_Biz10",   "Teacher4"),
            ("Placement60", "BT_Student26",  "BT_Biz10",   "Teacher4"),
            // BT_Biz11 (MersinSoft Yazılım): 2 öğrenci
            ("Placement61", "BT_Student27",  "BT_Biz11",   "Teacher3"),
            ("Placement62", "BT_Student28",  "BT_Biz11",   "Teacher3"),
            // BT_Biz12 (GameDev Studio): 2 öğrenci
            ("Placement63", "BT_Student29",  "BT_Biz12",   "Teacher4"),
            ("Placement64", "BT_Student30",  "BT_Biz12",   "Teacher4"),

            // ── MTT öğrencileri (30 öğrenci → 12 işletme) ──────────────────
            // Business3 (Toroslar CNC Makine): 2 öğrenci
            ("Placement9",  "MTT_Student1",  "Business3",  "Teacher5"),
            ("Placement10", "MTT_Student2",  "Business3",  "Teacher5"),
            // Business4 (Akdeniz Otomasyon): 2 öğrenci
            ("Placement11", "MTT_Student3",  "Business4",  "Teacher6"),
            ("Placement12", "MTT_Student4",  "Business4",  "Teacher6"),
            // MTT_Biz1 (Toroslar Makine Sanayi): 3 öğrenci
            ("Placement65", "MTT_Student5",  "MTT_Biz1",   "Teacher5"),
            ("Placement66", "MTT_Student6",  "MTT_Biz1",   "Teacher5"),
            ("Placement67", "MTT_Student7",  "MTT_Biz1",   "Teacher5"),
            // MTT_Biz2 (CNC Hassas İşleme): 2 öğrenci
            ("Placement68", "MTT_Student8",  "MTT_Biz2",   "Teacher5"),
            ("Placement69", "MTT_Student9",  "MTT_Biz2",   "Teacher5"),
            // MTT_Biz3 (Hidrolik Pnömatik): 2 öğrenci
            ("Placement70", "MTT_Student10", "MTT_Biz3",   "Teacher6"),
            ("Placement71", "MTT_Student11", "MTT_Biz3",   "Teacher6"),
            // MTT_Biz4 (Klima ve Soğutma): 2 öğrenci
            ("Placement72", "MTT_Student12", "MTT_Biz4",   "Teacher6"),
            ("Placement73", "MTT_Student13", "MTT_Biz4",   "Teacher6"),
            // MTT_Biz5 (Torna ve Freze): 2 öğrenci
            ("Placement74", "MTT_Student14", "MTT_Biz5",   "Teacher5"),
            ("Placement75", "MTT_Student15", "MTT_Biz5",   "Teacher5"),
            // MTT_Biz6 (Çelik Konstrüksiyon): 3 öğrenci
            ("Placement76", "MTT_Student16", "MTT_Biz6",   "Teacher6"),
            ("Placement77", "MTT_Student17", "MTT_Biz6",   "Teacher6"),
            ("Placement78", "MTT_Student18", "MTT_Biz6",   "Teacher6"),
            // MTT_Biz7 (Mersin Döküm Sanayi): 3 öğrenci
            ("Placement79", "MTT_Student19", "MTT_Biz7",   "Teacher5"),
            ("Placement80", "MTT_Student20", "MTT_Biz7",   "Teacher5"),
            ("Placement81", "MTT_Student21", "MTT_Biz7",   "Teacher5"),
            // MTT_Biz8 (Kaynak ve Metal): 2 öğrenci
            ("Placement82", "MTT_Student22", "MTT_Biz8",   "Teacher6"),
            ("Placement83", "MTT_Student23", "MTT_Biz8",   "Teacher6"),
            // MTT_Biz9 (Alüminyum Profil): 3 öğrenci
            ("Placement84", "MTT_Student24", "MTT_Biz9",   "Teacher5"),
            ("Placement85", "MTT_Student25", "MTT_Biz9",   "Teacher5"),
            ("Placement86", "MTT_Student26", "MTT_Biz9",   "Teacher5"),
            // MTT_Biz10 (Otomotiv Yedek Parça): 4 öğrenci
            ("Placement87", "MTT_Student27", "MTT_Biz10",  "Teacher6"),
            ("Placement88", "MTT_Student28", "MTT_Biz10",  "Teacher6"),
            ("Placement89", "MTT_Student29", "MTT_Biz10",  "Teacher6"),
            ("Placement90", "MTT_Student30", "MTT_Biz10",  "Teacher6"),
        };

        // Kalan kontenjanı önden oku (#80): kapasite bir domain kuralı, dolu işletmeye
        // yerleştirme 422 "İşletme kapasitesi dolu" döndürüyor. Plan bazı işletmeler için
        // kontenjandan fazla öğrenci içerdiğinden her koşuda 422 yağıyordu. Artık önden
        // bakıp atlıyoruz — bu bir hata değil, planın sınırı.
        var availableSlots = await GetAvailableSlotsAsync(api);

        var createdCount = 0;
        foreach (var p in placements)
        {
            if (!ctx.Has(p.StudentKey) || !ctx.Has(p.BusinessKey)) continue;

            var studentId = ctx.Get(p.StudentKey);
            if (placedStudents.Contains(studentId))
            {
                Console.WriteLine($"  → {p.StudentKey} zaten yerleştirilmiş, atlandı");
                continue;
            }

            var targetBusinessId = ctx.Get(p.BusinessKey);
            if (availableSlots.TryGetValue(targetBusinessId, out var slots) && slots <= 0)
            {
                Console.WriteLine($"  → {p.StudentKey} → {p.BusinessKey} atlandı (kontenjan dolu)");
                continue;
            }

            var body = new Dictionary<string, object>
            {
                ["studentId"] = studentId,
                ["businessId"] = ctx.Get(p.BusinessKey),
                ["institutionId"] = institutionId,
                ["academicPeriodId"] = academicPeriodId
            };
            if (p.TeacherKey is not null && ctx.Has(p.TeacherKey))
                body["teacherId"] = ctx.Get(p.TeacherKey);

            var data = await api.PostAsync("/api/placements", body);
            if (data is not null)
            {
                ctx.Set(p.Key, data.Value.GetProperty("id").GetGuid());
                createdCount++;
                if (availableSlots.ContainsKey(targetBusinessId))
                    availableSlots[targetBusinessId]--;
                Console.WriteLine($"  ✓ {p.StudentKey} → {p.BusinessKey} yerleştirildi");
            }
        }

        if (createdCount == 0)
            Console.WriteLine("  → Tüm yerleştirmeler zaten mevcut");

        await SeedSchoolPlacements(api, ctx, institutionId, academicPeriodId, placedStudents, allPlacements);
    }

    /// <summary>
    /// Yerleştirilmiş öğrenci kimlikleri — <b>tüm sayfalar</b> gezilerek.
    ///
    /// <para>Eskiden tek istekte <c>pageSize=200</c> soruluyordu; sunucu
    /// <c>PagedQuery.SafePageSize</c> ile bunu <b>100'e kırpıyor</b> ve fazlası sessizce
    /// düşüyordu. Sonuç: 100'ü aşan her yerleştirme "yok" sayılıyor, seeder o öğrenciyi
    /// yeniden yerleştirmeye çalışıyor ve her koşuda 422 "Yerleştirildi durumundan
    /// Yerleştirildi durumuna geçirilemez" yağıyordu.</para>
    /// </summary>
    /// <summary>
    /// Okulda staj (#159): staj yeri bulunamayan öğrenci stajını okulda yapar. İşletme YOKTUR —
    /// gövdede <c>businessId</c> hiç gönderilmez; ücret, devlet katkısı ve dekont doğmaz.
    ///
    /// <para><b>Neden seed ediliyor:</b> bu yol hiçbir ortamda gerçek veriyle çalışmıyordu.
    /// Yerleştirmelerin tamamı işletmeliydi, dolayısıyla <c>SchoolPlacedStudentView</c> hiç
    /// dolmuyor, okulda staj dönem notu ekranı (#171) boş kalıyor ve
    /// <c>ResyncPlacementProjections</c>'ın işverensiz dalı hiç yürütülmüyordu. Üç alandan
    /// birer öğrenci, alan kapsamı kararlarının da gerçek veriyle karşılanmasını sağlar.</para>
    ///
    /// <para><c>teacherId</c> burada koordinatör değil <b>gözetmendir</b> (alan/atölye şefi) ve
    /// ücret üretmez.</para>
    /// </summary>
    private static async Task SeedSchoolPlacements(
        MesnetApiClient api, SeedContext ctx, Guid institutionId, Guid academicPeriodId,
        HashSet<Guid> placedStudents, IReadOnlyList<System.Text.Json.JsonElement> allPlacements)
    {
        // Öğrenci anahtarı SABİTLENMEZ: yerleştirme planı ortamlar arasında kayabilir ve
        // sabit anahtar dolu çıkarsa okulda staj kaydı hiç oluşmaz. Her alandaki İLK boşta
        // öğrenci seçilir — boş veritabanında da, yıllardır koşan bir dev veritabanında da
        // çalışır.
        var branches = new (string Key, string BranchCode, string? TeacherKey)[]
        {
            ("SchoolPlacement1", "EET", "Teacher1"),
            ("SchoolPlacement2", "BT",  "Teacher2"),
            ("SchoolPlacement3", "MTT", "Teacher3"),
        };

        // Alanda zaten okulda staj varsa yenisi açılmaz. Kontrol öğrenci kimliğine DEĞİL
        // yerleştirmenin kendisine bakar: mükerrer öğrenci kaydı bulunan bir veritabanında
        // ad→kimlik eşlemesi koşudan koşuya kayar ve kimlik bazlı kontrol her seferinde
        // "boşta öğrenci" bulup yeni kayıt açardı.
        var okuldaStajliAlanlar = allPlacements
            .Where(p => p.TryGetProperty("businessId", out var b)
                        && b.ValueKind == System.Text.Json.JsonValueKind.Null)
            .Select(p => p.TryGetProperty("branchCode", out var c) ? c.GetString() : null)
            .Where(c => !string.IsNullOrEmpty(c))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var created = 0;
        var bosBulunamayan = new List<string>();

        foreach (var branch in branches)
        {
            if (okuldaStajliAlanlar.Contains(branch.BranchCode)) continue;

            var studentKey = FindUnplacedStudentKey(ctx, branch.BranchCode, placedStudents);
            if (studentKey is null)
            {
                bosBulunamayan.Add(branch.BranchCode);
                continue;
            }

            // businessId GÖNDERİLMEZ — okulda stajın tek ölçütü işletmenin yokluğudur.
            var body = new Dictionary<string, object>
            {
                ["studentId"] = ctx.Get(studentKey),
                ["institutionId"] = institutionId,
                ["academicPeriodId"] = academicPeriodId
            };
            if (branch.TeacherKey is not null && ctx.Has(branch.TeacherKey))
                body["teacherId"] = ctx.Get(branch.TeacherKey);

            var data = await api.PostAsync("/api/placements", body);
            if (data is null) continue;

            var placementId = data.Value.GetProperty("id").GetGuid();
            ctx.Set(branch.Key, placementId);
            placedStudents.Add(ctx.Get(studentKey));
            created++;
            Console.WriteLine($"  ✓ {studentKey} okulda staja yerleştirildi (işletmesiz)");
        }

        // "Hiç oluşturulmadı" ile "zaten vardı" AYNI ŞEY DEĞİLDİR — ilkini ikincisi diye
        // yazmak, başarısız çağrıyı başarı gibi gösterir.
        if (bosBulunamayan.Count > 0)
            Console.WriteLine(
                $"  ⚠ Okulda staj için boşta öğrenci kalmayan alan(lar): {string.Join(", ", bosBulunamayan)}");
        else if (created == 0)
            Console.WriteLine("  → Okulda staj yerleştirmeleri zaten mevcut");
    }

    /// <summary>Alandaki ilk yerleştirilmemiş öğrencinin anahtarı; hepsi doluysa <c>null</c>.</summary>
    private static string? FindUnplacedStudentKey(
        SeedContext ctx, string branchCode, HashSet<Guid> placedStudents)
    {
        for (var i = 1; i <= StudentsPerBranch; i++)
        {
            var key = $"{branchCode}_Student{i}";
            if (ctx.Has(key) && !placedStudents.Contains(ctx.Get(key)))
                return key;
        }

        return null;
    }

    /// <summary>Alan başına üretilen öğrenci sayısı — 12. sınıf iki şube (bkz. GenerateStudents).</summary>
    private const int StudentsPerBranch = 40;

    /// <summary>
    /// İşletme id → kalan kontenjan haritası. Kapasite bilgisi taşımayan işletme haritaya
    /// girmez; o durumda kontrol atlanır ve karar API'ye bırakılır.
    /// </summary>
    private static async Task<Dictionary<Guid, int>> GetAvailableSlotsAsync(MesnetApiClient api)
    {
        var slots = new Dictionary<Guid, int>();
        var businesses = await api.GetAllPagedAsync("/api/businesses");

        foreach (var b in businesses)
        {
            if (!b.TryGetProperty("id", out var idEl) || !idEl.TryGetGuid(out var id)) continue;
            if (!b.TryGetProperty("capacity", out var cap)
                || cap.ValueKind != System.Text.Json.JsonValueKind.Object) continue;
            if (cap.TryGetProperty("availableSlots", out var avail) && avail.TryGetInt32(out var n))
                slots[id] = n;
        }

        return slots;
    }

    private static void AliasIfMissing(SeedContext ctx, string alias, string source)
    {
        if (!ctx.Has(alias) && ctx.Has(source))
            ctx.Set(alias, ctx.Get(source));
    }

    private sealed record StudentData(
        string Key,
        string KcId,
        string Name,
        string BranchCode,
        string BranchName,
        int ClassYear,
        string Section,
        string EducationType,
        string StudentNumber);
}
