namespace MESNET.Seeder.Seeders;

public static class BusinessSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── İşletmeler ─────────────────────");

        if (!ctx.Has("Institution")) return;
        var tenantId = ctx.Get("Institution");

        // Mevcut işletmeleri yükle — ada göre eşleştir
        var existing = await api.GetAsync("/api/businesses");
        var existingByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (existing is { } arr && arr.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in arr.EnumerateArray())
            {
                var name = item.GetProperty("name").GetString() ?? "";
                var id = item.GetProperty("id").GetGuid();
                existingByName[name] = id;
            }
        }

        var businessSectors = new Dictionary<string, string[]>
        {
            ["Business1"] = ["InformationTechnology"],
            ["Business2"] = ["ElectricalAndElectronics", "Machinery"],
            ["Business3"] = ["InformationTechnology"],
            ["Business4"] = ["Finance", "BusinessAndManagement"],
        };

        await SeedBusiness(api, ctx, existingByName, "Business1", "Bilge Yazılım A.Ş.", () => api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Bilge Yazılım A.Ş.",
            address = "Çankaya, Ankara",
            phoneNumber = "0312 444 1001",
            email = "info@bilgeyazilim.com",
            website = "https://bilgeyazilim.com",
            personnelCount = 25,
            location = new { latitude = 39.9208, longitude = 32.8541 },
            totalSlots = 5,
            sectors = new[] { "InformationTechnology" }
        }));

        await SeedBusiness(api, ctx, existingByName, "Business2", "Anadolu Otomasyon Ltd. Şti.", () => api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Anadolu Otomasyon Ltd. Şti.",
            address = "OSTİM, Ankara",
            phoneNumber = "0312 444 2002",
            email = "info@anadoluotomasyon.com",
            personnelCount = 45,
            location = new { latitude = 39.9725, longitude = 32.7398 },
            totalSlots = 8,
            sectors = new[] { "ElectricalAndElectronics", "Machinery" }
        }));

        await SeedBusiness(api, ctx, existingByName, "Business3", "Yeni Nesil Teknoloji", () => api.PostAsync("/api/businesses/self-register", new
        {
            tenantId,
            keycloakId = "53000000-0000-0000-0000-000000000001",
            fullName = "Can Özkan",
            representativePhone = "0555 123 4567",
            representativeEmail = "can@yeninesitek.com",
            businessName = "Yeni Nesil Teknoloji",
            address = "Kızılay, Ankara",
            phoneNumber = "0312 444 3003",
            email = "info@yeninesitek.com",
            personnelCount = 8,
            location = new { latitude = 39.9255, longitude = 32.8658 },
            totalSlots = 3,
            sectors = new[] { "InformationTechnology" }
        }));

        await SeedBusiness(api, ctx, existingByName, "Business4", "Öz-Er Muhasebe ve Danışmanlık", () => api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Öz-Er Muhasebe ve Danışmanlık",
            address = "Kavaklıdere, Ankara",
            phoneNumber = "0312 444 4004",
            email = "info@ozermuhasebe.com",
            personnelCount = 12,
            location = new { latitude = 39.9100, longitude = 32.8600 },
            totalSlots = 4,
            sectors = new[] { "Finance", "BusinessAndManagement" }
        }));

        // Mevcut işletmelere sektör bilgisi yoksa PATCH ile ekle
        foreach (var (ctxKey, sectors) in businessSectors)
        {
            if (!ctx.Has(ctxKey)) continue;
            var bizId = ctx.Get(ctxKey);
            if (!existingByName.ContainsValue(bizId)) continue; // yeni oluşturulan, zaten sektörlü

            await api.PatchAsync($"/api/businesses/{bizId}", new { sectors });
            Console.WriteLine($"  ↻ \"{ctxKey}\" sektörler güncellendi");
        }

        // Herhangi bir yeni işletme oluşturulmuşsa, Enrollment modülünün event'i işlemesi için bekle
        var newlyCreated = new[] { "Business1", "Business2", "Business3", "Business4" }
            .Any(k => ctx.Has(k) && !existingByName.ContainsValue(ctx.Get(k)));
        if (newlyCreated)
        {
            Console.WriteLine("  … Enrollment event consumer bekleniyor (3s)...");
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    private static async Task SeedBusiness(
        MesnetApiClient api, SeedContext ctx,
        Dictionary<string, Guid> existingByName,
        string ctxKey, string name,
        Func<Task<System.Text.Json.JsonElement?>> createFn)
    {
        if (existingByName.TryGetValue(name, out var existingId))
        {
            ctx.Set(ctxKey, existingId);
            Console.WriteLine($"  → \"{name}\" mevcut, yüklendi");
            return;
        }

        var data = await createFn();
        if (data is not null)
        {
            ctx.Set(ctxKey, data.Value.GetProperty("id").GetGuid());
            Console.WriteLine($"  ✓ \"{name}\" oluşturuldu");
        }
    }
}
