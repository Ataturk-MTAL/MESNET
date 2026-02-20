namespace MESNET.Seeder.Seeders;

public static class EnrollmentSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        if (!ctx.Has("Institution")) return;
        var institutionId = ctx.Get("Institution");

        await SeedTeachers(api, ctx, institutionId);
        await SeedStudents(api, ctx, institutionId);
        await SeedPlacements(api, ctx, institutionId);
    }

    private static async Task SeedTeachers(MesnetApiClient api, SeedContext ctx, Guid institutionId)
    {
        Console.WriteLine();
        Console.WriteLine("── Öğretmenler ────────────────────");

        var teachers = new[]
        {
            ("Teacher1", "51000000-0000-0000-0000-000000000001", "Ayşe Çelik"),
            ("Teacher2", "51000000-0000-0000-0000-000000000002", "Mustafa Yılmaz"),
            ("Teacher3", "51000000-0000-0000-0000-000000000003", "Hasan Kara")
        };

        foreach (var (key, kcId, name) in teachers)
        {
            var data = await api.PostAsync("/api/teachers", new
            {
                institutionId,
                keycloakUserId = Guid.Parse(kcId),
                fullName = name
            });
            if (data is not null)
            {
                ctx.Set(key, data.Value.GetProperty("id").GetGuid());
                Console.WriteLine($"  ✓ Öğretmen \"{name}\" kaydedildi");
            }
        }
    }

    private static async Task SeedStudents(MesnetApiClient api, SeedContext ctx, Guid institutionId)
    {
        Console.WriteLine();
        Console.WriteLine("── Öğrenciler ─────────────────────");

        var students = new (string Key, string KcId, string Name, string BranchCode, string BranchName, int ClassYear, string? Section)[]
        {
            ("Student1", "41000000-0000-0000-0000-000000000001", "Elif Demir", "BT", "Bilişim Teknolojileri", 11, "A"),
            ("Student2", "41000000-0000-0000-0000-000000000002", "Can Yıldız", "BT", "Bilişim Teknolojileri", 11, "A"),
            ("Student3", "41000000-0000-0000-0000-000000000003", "Ceren Aksoy", "BT", "Bilişim Teknolojileri", 12, "B"),
            ("Student4", "41000000-0000-0000-0000-000000000004", "Burak Şahin", "BT", "Bilişim Teknolojileri", 11, "B"),
            ("Student5", "41000000-0000-0000-0000-000000000005", "Deniz Koç", "BT", "Bilişim Teknolojileri", 12, "A"),
            ("Student6", "41000000-0000-0000-0000-000000000006", "Selin Aydın", "MUF", "Muhasebe ve Finansman", 11, "A"),
            ("Student7", "41000000-0000-0000-0000-000000000007", "Emre Polat", "MUF", "Muhasebe ve Finansman", 12, "A"),
            ("Student8", "41000000-0000-0000-0000-000000000008", "Aylin Türk", "MUF", "Muhasebe ve Finansman", 11, "B")
        };

        foreach (var s in students)
        {
            var data = await api.PostAsync("/api/students", new
            {
                institutionId,
                keycloakUserId = Guid.Parse(s.KcId),
                fullName = s.Name,
                branchCode = s.BranchCode,
                branchName = s.BranchName,
                classYear = s.ClassYear,
                section = s.Section
            });
            if (data is not null)
            {
                ctx.Set(s.Key, data.Value.GetProperty("id").GetGuid());
                Console.WriteLine($"  ✓ Öğrenci \"{s.Name}\" ({s.BranchCode}, {s.ClassYear}/{s.Section}) kaydedildi");
            }
        }
    }

    private static async Task SeedPlacements(MesnetApiClient api, SeedContext ctx, Guid institutionId)
    {
        Console.WriteLine();
        Console.WriteLine("── Yerleştirmeler ─────────────────");

        var placements = new (string Key, string StudentKey, string BusinessKey, string? TeacherKey)[]
        {
            ("Placement1", "Student1", "Business1", "Teacher1"),
            ("Placement2", "Student2", "Business2", "Teacher2"),
            ("Placement3", "Student3", "Business1", "Teacher1"),
            ("Placement4", "Student6", "Business4", "Teacher3"),
            ("Placement5", "Student7", "Business4", "Teacher3")
        };

        foreach (var p in placements)
        {
            if (!ctx.Has(p.StudentKey) || !ctx.Has(p.BusinessKey)) continue;

            var body = new Dictionary<string, object>
            {
                ["studentId"] = ctx.Get(p.StudentKey),
                ["businessId"] = ctx.Get(p.BusinessKey),
                ["institutionId"] = institutionId
            };
            if (p.TeacherKey is not null && ctx.Has(p.TeacherKey))
                body["teacherId"] = ctx.Get(p.TeacherKey);

            var data = await api.PostAsync("/api/placements", body);
            if (data is not null)
            {
                ctx.Set(p.Key, data.Value.GetProperty("id").GetGuid());
                Console.WriteLine($"  ✓ {p.StudentKey} → {p.BusinessKey} yerleştirildi");
            }
        }
    }
}
