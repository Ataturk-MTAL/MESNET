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
        var existing = await api.GetAsync($"/api/teachers?institutionId={institutionId}&pageSize=100");
        var existingByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (existing is { } pagedResult
            && pagedResult.TryGetProperty("items", out var itemsEl)
            && itemsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                var name = item.GetProperty("fullName").GetString() ?? "";
                var id = item.GetProperty("id").GetGuid();
                existingByName[name] = id;
            }
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

        // Mevcut öğrencileri yükle — GetAsync → envelope.Data = PagedResult { items: [...] }
        var existing = await api.GetAsync($"/api/students?institutionId={institutionId}&pageSize=200");
        var existingByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (existing is { } pagedResult
            && pagedResult.TryGetProperty("items", out var itemsEl)
            && itemsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                var name = item.GetProperty("fullName").GetString() ?? "";
                var id = item.GetProperty("id").GetGuid();
                existingByName[name] = id;
            }
        }

        // 120 öğrenci: 3 alan × 40 öğrenci (12/A: 20, 12/B: 20)
        // Norm Kadro Yönetmeliği Madde 22: 12. sınıf 20 öğrenci → 2 grup
        var students = GenerateStudents();
        var createdCount = 0;

        foreach (var s in students)
        {
            if (existingByName.TryGetValue(s.Name, out var existingId))
            {
                ctx.Set(s.Key, existingId);
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
                educationType = s.EducationType
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
            Console.WriteLine($"  ✓ Toplam {createdCount} yeni öğrenci oluşturuldu (120 hedef)");
        else
            Console.WriteLine("  → Tüm öğrenciler zaten mevcut");
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
        for (var i = 0; i < 40; i++)
        {
            var section = i < 20 ? "A" : "B";
            var seqNum = i + 1;
            students.Add(new StudentData(
                $"EET_Student{seqNum}",
                $"41000000-0000-0000-0000-{kcIdCounter++:D12}",
                eetNames[i],
                "EET",
                "Elektrik-Elektronik Teknolojisi",
                12,
                section,
                "Formal"
            ));
        }

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
        for (var i = 0; i < 40; i++)
        {
            var section = i < 20 ? "A" : "B";
            var seqNum = i + 1;
            students.Add(new StudentData(
                $"BT_Student{seqNum}",
                $"41000000-0000-0000-0000-{kcIdCounter++:D12}",
                btNames[i],
                "BT",
                "Bilişim Teknolojileri",
                12,
                section,
                "Formal"
            ));
        }

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
        for (var i = 0; i < 40; i++)
        {
            var section = i < 20 ? "A" : "B";
            var seqNum = i + 1;
            students.Add(new StudentData(
                $"MTT_Student{seqNum}",
                $"41000000-0000-0000-0000-{kcIdCounter++:D12}",
                mttNames[i],
                "MTT",
                "Makine ve Tasarım Teknolojisi",
                12,
                section,
                "Formal"
            ));
        }

        return students;
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

        // Mevcut yerleştirmeleri yükle — GetAsync → envelope.Data = PagedResult { items: [...] }
        var existing = await api.GetAsync("/api/placements?pageSize=200");
        var placedStudents = new HashSet<Guid>();
        if (existing is { } pagedResult
            && pagedResult.TryGetProperty("items", out var itemsEl)
            && itemsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
                placedStudents.Add(item.GetProperty("studentId").GetGuid());
        }

        // Her alandan birkaç öğrenciyi yerleştir (sözleşme/devamsızlık/koordinasyon seeder'ları için)
        var placements = new (string Key, string StudentKey, string BusinessKey, string? TeacherKey)[]
        {
            // EET öğrencileri → Business1 (Elektrik) & Business4 (Otomasyon)
            ("Placement1",  "EET_Student1",  "Business1", "Teacher1"),
            ("Placement2",  "EET_Student2",  "Business1", "Teacher1"),
            ("Placement3",  "EET_Student3",  "Business4", "Teacher2"),
            ("Placement4",  "EET_Student4",  "Business4", "Teacher2"),
            // BT öğrencileri → Business2 (Bilişim) & Business5 (Yeni Nesil)
            ("Placement5",  "BT_Student1",   "Business2", "Teacher3"),
            ("Placement6",  "BT_Student2",   "Business2", "Teacher3"),
            ("Placement7",  "BT_Student3",   "Business5", "Teacher4"),
            ("Placement8",  "BT_Student4",   "Business5", "Teacher4"),
            // MTT öğrencileri → Business3 (CNC Makine) & Business4 (Otomasyon)
            ("Placement9",  "MTT_Student1",  "Business3", "Teacher5"),
            ("Placement10", "MTT_Student2",  "Business3", "Teacher5"),
            ("Placement11", "MTT_Student3",  "Business4", "Teacher6"),
            ("Placement12", "MTT_Student4",  "Business4", "Teacher6"),
        };

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
                Console.WriteLine($"  ✓ {p.StudentKey} → {p.BusinessKey} yerleştirildi");
            }
        }

        if (createdCount == 0)
            Console.WriteLine("  → Tüm yerleştirmeler zaten mevcut");
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
        string EducationType);
}
