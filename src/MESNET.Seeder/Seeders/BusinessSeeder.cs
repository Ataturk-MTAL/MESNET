namespace MESNET.Seeder.Seeders;

public static class BusinessSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── İşletmeler ─────────────────────");

        if (!ctx.Has("Institution")) return;
        var tenantId = ctx.Get("Institution");

        // Business 1: Bilge Yazılım — Active (kurum kaydı)
        var d1 = await api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Bilge Yazılım A.Ş.",
            address = "Çankaya, Ankara",
            phoneNumber = "0312 444 1001",
            email = "info@bilgeyazilim.com",
            website = "https://bilgeyazilim.com",
            personnelCount = 25,
            location = new { latitude = 39.9208, longitude = 32.8541 },
            totalSlots = 5
        });
        if (d1 is not null)
        {
            ctx.Set("Business1", d1.Value.GetProperty("id").GetGuid());
            Console.WriteLine($"  ✓ \"Bilge Yazılım A.Ş.\" kaydedildi (Active)");
        }

        // Business 2: Anadolu Otomasyon — Active
        var d2 = await api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Anadolu Otomasyon Ltd. Şti.",
            address = "OSTİM, Ankara",
            phoneNumber = "0312 444 2002",
            email = "info@anadoluotomasyon.com",
            personnelCount = 45,
            location = new { latitude = 39.9725, longitude = 32.7398 },
            totalSlots = 8
        });
        if (d2 is not null)
        {
            ctx.Set("Business2", d2.Value.GetProperty("id").GetGuid());
            Console.WriteLine($"  ✓ \"Anadolu Otomasyon Ltd.\" kaydedildi (Active)");
        }

        // Business 3: Yeni Nesil Teknoloji — SelfRegistered → PendingApproval
        var d3 = await api.PostAsync("/api/businesses/self-register", new
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
            totalSlots = 3
        });
        if (d3 is not null)
        {
            ctx.Set("Business3", d3.Value.GetProperty("id").GetGuid());
            Console.WriteLine($"  ✓ \"Yeni Nesil Teknoloji\" self-register (Onay Bekliyor)");
        }

        // Business 4: Öz-Er Muhasebe — Active
        var d4 = await api.PostAsync("/api/businesses", new
        {
            tenantId,
            name = "Öz-Er Muhasebe ve Danışmanlık",
            address = "Kavaklıdere, Ankara",
            phoneNumber = "0312 444 4004",
            email = "info@ozermuhasebe.com",
            personnelCount = 12,
            location = new { latitude = 39.9100, longitude = 32.8600 },
            totalSlots = 4
        });
        if (d4 is not null)
        {
            ctx.Set("Business4", d4.Value.GetProperty("id").GetGuid());
            Console.WriteLine($"  ✓ \"Öz-Er Muhasebe\" kaydedildi (Active)");
        }
    }
}
