namespace MESNET.Seeder.Seeders;

public static class ContractSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── Sözleşmeler ────────────────────");

        if (!ctx.Has("Institution")) return;
        var institutionId = ctx.Get("Institution");

        // Mevcut sözleşmeleri yükle — studentId'ye göre eşleştir
        var existing = await api.GetAsync($"/api/contracts?institutionId={institutionId}");
        var existingByStudent = new Dictionary<Guid, Guid>(); // studentId → contractId
        if (existing is { } arr && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var studentId = item.GetProperty("studentId").GetGuid();
                var contractId = item.GetProperty("id").GetGuid();
                existingByStudent[studentId] = contractId;
            }
        }

        var now = DateTime.UtcNow;
        await SeedContract1(api, ctx, institutionId, now, existingByStudent);
        await SeedContract2(api, ctx, institutionId, now, existingByStudent);
        await SeedContract3(api, ctx, institutionId, now, existingByStudent);
        await SeedContract4(api, ctx, institutionId, now, existingByStudent);
    }

    private static async Task SeedContract1(
        MesnetApiClient api, SeedContext ctx, Guid institutionId, DateTime now,
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
            teacherId = ctx.Has("Teacher1") ? ctx.Get("Teacher1") : (Guid?)null,
            startDate = now.AddMonths(-2),
            endDate = now.AddMonths(4)
        });
        if (data is null) return;

        var contractId = data.Value.GetProperty("contractId").GetGuid();
        ctx.Set("Contract1", contractId);

        await api.PostAsync($"/api/contracts/{contractId}/submit");
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Institution", signedBy = "Ahmet Yılmaz" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Business", signedBy = "Mehmet Kaya" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Student", signedBy = "Elif Demir" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Parent", signedBy = "Fatma Demir" });
        await api.PostAsync($"/api/contracts/{contractId}/activate");
        Console.WriteLine("  ✓ Sözleşme 1: oluşturuldu → imza → aktif");
    }

    private static async Task SeedContract2(
        MesnetApiClient api, SeedContext ctx, Guid institutionId, DateTime now,
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
            teacherId = ctx.Has("Teacher2") ? ctx.Get("Teacher2") : (Guid?)null,
            startDate = now.AddMonths(-1),
            endDate = now.AddMonths(5)
        });
        if (data is null) return;

        var contractId = data.Value.GetProperty("contractId").GetGuid();
        ctx.Set("Contract2", contractId);

        await api.PostAsync($"/api/contracts/{contractId}/submit");
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Institution", signedBy = "Ahmet Yılmaz" });
        Console.WriteLine("  ✓ Sözleşme 2: oluşturuldu → imza bekliyor (kurum imzaladı)");
    }

    private static async Task SeedContract3(
        MesnetApiClient api, SeedContext ctx, Guid institutionId, DateTime now,
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
            teacherId = ctx.Has("Teacher2") ? ctx.Get("Teacher2") : (Guid?)null,
            startDate = now.AddDays(-5),
            endDate = now.AddMonths(6)
        });
        if (data is null) return;

        ctx.Set("Contract3", data.Value.GetProperty("contractId").GetGuid());
        Console.WriteLine("  ✓ Sözleşme 3: taslak olarak oluşturuldu");
    }

    private static async Task SeedContract4(
        MesnetApiClient api, SeedContext ctx, Guid institutionId, DateTime now,
        Dictionary<Guid, Guid> existingByStudent)
    {
        if (!ctx.Has("Student3") || !ctx.Has("Business1")) return;
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
            businessId = ctx.Get("Business1"),
            institutionId,
            teacherId = ctx.Has("Teacher1") ? ctx.Get("Teacher1") : (Guid?)null,
            startDate = new DateTime(2024, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            endDate = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc)
        });
        if (data is null) return;

        var contractId = data.Value.GetProperty("contractId").GetGuid();
        ctx.Set("Contract4", contractId);

        await api.PostAsync($"/api/contracts/{contractId}/submit");
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Institution", signedBy = "Ahmet Yılmaz" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Business", signedBy = "Mehmet Kaya" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Student", signedBy = "Ceren Aksoy" });
        await api.PostAsync($"/api/contracts/{contractId}/sign", new { internshipContractId = contractId, party = "Parent", signedBy = "Murat Aksoy" });
        await api.PostAsync($"/api/contracts/{contractId}/activate");
        await api.PostAsync($"/api/contracts/{contractId}/complete");
        Console.WriteLine("  ✓ Sözleşme 4: oluşturuldu → imza → aktif → tamamlandı");
    }
}
