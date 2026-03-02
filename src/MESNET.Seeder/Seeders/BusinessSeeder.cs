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
            ["Business5"] = ["Finance", "BusinessAndManagement"],
        };

        await SeedBusiness(api, ctx, existingByName, "Business1", "Mersin Bilişim Teknolojileri A.Ş.", () => api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Mersin Bilişim Teknolojileri A.Ş.",
            address = "Çiftlikköy Mah., Yenişehir, Mersin",
            phoneNumber = "0324 444 1001",
            email = "info@mersinbt.com",
            website = "https://mersinbt.com",
            personnelCount = 25,
            location = new { latitude = 36.8005, longitude = 34.6421 },
            totalSlots = 5,
            sectors = new[] { "InformationTechnology" }
        }));

        await SeedBusiness(api, ctx, existingByName, "Business2", "Akdeniz Yazılım ve Danışmanlık Ltd. Şti.", () => api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Akdeniz Yazılım ve Danışmanlık Ltd. Şti.",
            address = "Mezitli Mah., Mezitli, Mersin",
            phoneNumber = "0324 444 2002",
            email = "info@akdenizyazilim.com",
            personnelCount = 18,
            location = new { latitude = 36.7812, longitude = 34.5534 },
            totalSlots = 8,
            sectors = new[] { "InformationTechnology" }
        }));

        await SeedBusiness(api, ctx, existingByName, "Business3", "Yeni Nesil Teknoloji", () => api.PostAsync("/api/businesses/self-register", new
        {
            tenantId,
            keycloakId = "53000000-0000-0000-0000-000000000001",
            fullName = "Can Özkan",
            representativePhone = "0555 123 4567",
            representativeEmail = "can@yeninesitek.com",
            businessName = "Yeni Nesil Teknoloji",
            address = "Güneykent Mah., Toroslar, Mersin",
            phoneNumber = "0324 444 3003",
            email = "info@yeninesitek.com",
            personnelCount = 8,
            location = new { latitude = 36.8134, longitude = 34.6287 },
            totalSlots = 3,
            sectors = new[] { "InformationTechnology" }
        }));

        await SeedBusiness(api, ctx, existingByName, "Business4", "Öz-Er Muhasebe ve Danışmanlık", () => api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Öz-Er Muhasebe ve Danışmanlık",
            address = "Bahçe Mah., Akdeniz, Mersin",
            phoneNumber = "0324 444 4004",
            email = "info@ozermuhasebe.com",
            personnelCount = 12,
            location = new { latitude = 36.8021, longitude = 34.6152 },
            totalSlots = 4,
            sectors = new[] { "Finance", "BusinessAndManagement" }
        }));

        await SeedBusiness(api, ctx, existingByName, "Business5", "Mersin Ticaret ve Sanayi Odası", () => api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Mersin Ticaret ve Sanayi Odası",
            address = "Atatürk Cad. MTSO Hizmet Binası, Akdeniz, Mersin",
            phoneNumber = "0324 238 9800",
            email = "info@mtso.org.tr",
            website = "https://www.mtso.org.tr",
            personnelCount = 60,
            location = new { latitude = 36.8065, longitude = 34.6392 },
            totalSlots = 6,
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
