namespace MESNET.Seeder.Seeders;

public static class ContractSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── Sözleşmeler ────────────────────");

        if (!ctx.Has("Institution") || !ctx.Has("AcademicPeriod")) return;
        var institutionId = ctx.Get("Institution");
        var academicPeriodId = ctx.Get("AcademicPeriod");

        // Mevcut sözleşmeleri yükle — GetAsync → envelope.Data = PagedResult { items: [...] }
        var existing = await api.GetAsync($"/api/contracts?institutionId={institutionId}&pageSize=100");
        var existingByStudent = new Dictionary<Guid, Guid>(); // studentId → contractId
        if (existing is { } pagedResult
            && pagedResult.TryGetProperty("items", out var itemsEl)
            && itemsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                var studentId = item.GetProperty("studentId").GetGuid();
                var contractId = item.GetProperty("id").GetGuid();
                existingByStudent[studentId] = contractId;
            }
        }

        var now = DateTime.UtcNow;
        await SeedContract1(api, ctx, institutionId, academicPeriodId, now, existingByStudent);
        await SeedContract2(api, ctx, institutionId, academicPeriodId, now, existingByStudent);
        await SeedContract3(api, ctx, institutionId, academicPeriodId, now, existingByStudent);
        await SeedContract4(api, ctx, institutionId, academicPeriodId, now, existingByStudent);
    }

    private static async Task SeedContract1(
        MesnetApiClient api, SeedContext ctx, Guid institutionId, Guid academicPeriodId, DateTime now,
        Dictionary<Guid, Guid> existingByStudent)
    {
        if (!ctx.Has("Student1") || !ctx.Has("Business1")) return;
        var studentId = ctx.Get("Student1");

        if (existingByStudent.TryGetValue(studentId, out var existingId))
        {
            ctx.Set("Contract1", existingId);
            Console.WriteLine("  → Sözleşme 1 mevcut, yüklendi");
            return;
        }

        var data = await api.PostAsync("/api/contracts", new
        {
            studentId,
            businessId = ctx.Get("Business1"),
            institutionId,
            academicPeriodId,
            teacherId = ctx.Has("Teacher1") ? ctx.Get("Teacher1") : (Guid?)null,
            startDate = now.AddMonths(-2)
        });
        if (data is null) return;

        var contractId = data.Value.GetProperty("contractId").GetGuid();
        ctx.Set("Contract1", contractId);

        await api.PostAsync($"/api/contracts/{contractId}/submit");
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Institution", signedBy = "Ahmet Yılmaz" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Business", signedBy = "Mehmet Kaya" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Student", signedBy = "Ahmet Yıldırım" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Parent", signedBy = "Fatma Yıldırım" });
        await api.PostAsync($"/api/contracts/{contractId}/activate");
        Console.WriteLine("  ✓ Sözleşme 1: oluşturuldu → imza → aktif (EET öğrenci)");
    }

    private static async Task SeedContract2(
        MesnetApiClient api, SeedContext ctx, Guid institutionId, Guid academicPeriodId, DateTime now,
        Dictionary<Guid, Guid> existingByStudent)
    {
        if (!ctx.Has("Student2") || !ctx.Has("Business2")) return;
        var studentId = ctx.Get("Student2");

        if (existingByStudent.TryGetValue(studentId, out var existingId))
        {
            ctx.Set("Contract2", existingId);
            Console.WriteLine("  → Sözleşme 2 mevcut, yüklendi");
            return;
        }

        var data = await api.PostAsync("/api/contracts", new
        {
            studentId,
            businessId = ctx.Get("Business2"),
            institutionId,
            academicPeriodId,
            teacherId = ctx.Has("Teacher3") ? ctx.Get("Teacher3") : (Guid?)null,
            startDate = now.AddMonths(-1)
        });
        if (data is null) return;

        var contractId = data.Value.GetProperty("contractId").GetGuid();
        ctx.Set("Contract2", contractId);

        await api.PostAsync($"/api/contracts/{contractId}/submit");
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Institution", signedBy = "Ahmet Yılmaz" });
        Console.WriteLine("  ✓ Sözleşme 2: oluşturuldu → imza bekliyor (BT öğrenci)");
    }

    private static async Task SeedContract3(
        MesnetApiClient api, SeedContext ctx, Guid institutionId, Guid academicPeriodId, DateTime now,
        Dictionary<Guid, Guid> existingByStudent)
    {
        if (!ctx.Has("Student4") || !ctx.Has("Business2")) return;
        var studentId = ctx.Get("Student4");

        if (existingByStudent.TryGetValue(studentId, out var existingId))
        {
            ctx.Set("Contract3", existingId);
            Console.WriteLine("  → Sözleşme 3 mevcut, yüklendi");
            return;
        }

        var data = await api.PostAsync("/api/contracts", new
        {
            studentId,
            businessId = ctx.Get("Business2"),
            institutionId,
            academicPeriodId,
            teacherId = ctx.Has("Teacher4") ? ctx.Get("Teacher4") : (Guid?)null,
            startDate = now.AddDays(-5)
        });
        if (data is null) return;

        ctx.Set("Contract3", data.Value.GetProperty("contractId").GetGuid());
        Console.WriteLine("  ✓ Sözleşme 3: taslak (BT öğrenci)");
    }

    private static async Task SeedContract4(
        MesnetApiClient api, SeedContext ctx, Guid institutionId, Guid academicPeriodId, DateTime now,
        Dictionary<Guid, Guid> existingByStudent)
    {
        if (!ctx.Has("Student3") || !ctx.Has("Business5")) return;
        var studentId = ctx.Get("Student3");

        if (existingByStudent.TryGetValue(studentId, out var existingId))
        {
            ctx.Set("Contract4", existingId);
            Console.WriteLine("  → Sözleşme 4 mevcut, yüklendi");
            return;
        }

        var data = await api.PostAsync("/api/contracts", new
        {
            studentId,
            businessId = ctx.Get("Business5"),
            institutionId,
            academicPeriodId,
            teacherId = ctx.Has("Teacher4") ? ctx.Get("Teacher4") : (Guid?)null,
            startDate = new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        if (data is null) return;

        var contractId = data.Value.GetProperty("contractId").GetGuid();
        ctx.Set("Contract4", contractId);

        await api.PostAsync($"/api/contracts/{contractId}/submit");
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Institution", signedBy = "Ahmet Yılmaz" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Business", signedBy = "Can Özkan" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Student", signedBy = "Ceren Aksoy" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Parent", signedBy = "Murat Aksoy" });
        await api.PostAsync($"/api/contracts/{contractId}/activate");
        await api.PostAsync($"/api/contracts/{contractId}/complete");
        Console.WriteLine("  ✓ Sözleşme 4: oluşturuldu → imza → aktif → tamamlandı (BT öğrenci)");
    }
}
