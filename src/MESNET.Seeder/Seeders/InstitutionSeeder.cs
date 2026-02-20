namespace MESNET.Seeder.Seeders;

public static class InstitutionSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── Kurum ──────────────────────────");

        // İdempotency: İşletme varsa zaten seed edilmiş
        var existing = await api.GetAsync("/api/businesses");
        if (existing is { } el && el.ValueKind == System.Text.Json.JsonValueKind.Array
                               && el.GetArrayLength() > 0)
        {
            Console.WriteLine("  ⊘ Veriler zaten mevcut, atlanıyor.");
            return;
        }

        // 1. Kurum oluştur
        var data = await api.PostAsync("/api/institutions", new
        {
            tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            institutionCode = 123456,
            fullName = "Ankara Mesleki ve Teknik Anadolu Lisesi",
            address = "Yenimahalle, Ankara",
            phoneNumber = "0312 555 0001",
            email = "ankara.mtal@meb.gov.tr",
            webUrl = "https://ankaramtal.meb.k12.tr",
            location = new { latitude = 39.9334, longitude = 32.8597 }
        });

        if (data is null) return;
        var institutionId = data.Value.GetProperty("id").GetGuid();
        ctx.Set("Institution", institutionId);
        Console.WriteLine($"  ✓ Kurum oluşturuldu (id: {institutionId.ToString()[..8]}...)");

        // 2. Alanları aktifleştir
        await api.PostAsync($"/api/institutions/{institutionId}/branches", new { fieldCode = "BT" });
        Console.WriteLine("  ✓ Alan \"BT\" aktifleştirildi");

        await api.PostAsync($"/api/institutions/{institutionId}/branches", new { fieldCode = "MUF" });
        Console.WriteLine("  ✓ Alan \"MUF\" aktifleştirildi");

        // 3. Personel ekle
        var staffMembers = new[]
        {
            new { keycloakId = "52000000-0000-0000-0000-000000000001", fullName = "Ahmet Yılmaz", role = "Principal", branchCode = (string?)null },
            new { keycloakId = "52000000-0000-0000-0000-000000000002", fullName = "Zeynep Arslan", role = "VicePrincipal", branchCode = (string?)null },
            new { keycloakId = "52000000-0000-0000-0000-000000000003", fullName = "Fatih Demir", role = "Coordinator", branchCode = (string?)"BT" },
            new { keycloakId = "52000000-0000-0000-0000-000000000004", fullName = "Seda Kara", role = "Coordinator", branchCode = (string?)"MUF" },
            new { keycloakId = "52000000-0000-0000-0000-000000000005", fullName = "Emre Çetin", role = "Staff", branchCode = (string?)null }
        };

        foreach (var s in staffMembers)
        {
            await api.PostAsync($"/api/institutions/{institutionId}/staff", s);
            Console.WriteLine($"  ✓ Personel \"{s.fullName}\" ({s.role}) eklendi");
        }

        // 4. Ders programı ayarları
        await api.PutAsync($"/api/institutions/{institutionId}/schedule-config", new
        {
            institutionId,
            dailyPeriodCount = 8,
            updatedBy = "Sistem"
        });
        Console.WriteLine("  ✓ Ders programı (8 ders) ayarlandı");
    }
}
