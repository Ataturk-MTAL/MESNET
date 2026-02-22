namespace MESNET.Seeder.Seeders;

public static class InstitutionSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx, KeycloakAdminService keycloak)
    {
        Console.WriteLine();
        Console.WriteLine("── Kurum ──────────────────────────");

        // Mevcut kurumu kontrol et
        var existing = await api.GetAsync("/api/institutions");
        if (existing is { } arr && arr.ValueKind == System.Text.Json.JsonValueKind.Array && arr.GetArrayLength() > 0)
        {
            var inst = arr[0];
            var existingId = inst.GetProperty("id").GetGuid();
            ctx.Set("Institution", existingId);
            Console.WriteLine($"  → Kurum mevcut (id: {existingId.ToString()[..8]}...), yüklendi");

            // Keycloak'ı senkronize et
            try
            {
                await keycloak.UpdateAllUsersInstitutionIdAsync(existingId);
                Console.WriteLine("  ✓ Keycloak institution_id senkronize edildi");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠ Keycloak senkronizasyon başarısız: {ex.Message}");
            }

            // Aktif dönem yoksa oluştur
            await EnsureAcademicPeriodAsync(api, existingId);
            return;
        }

        // Kurum oluştur
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

        try
        {
            await keycloak.UpdateAllUsersInstitutionIdAsync(institutionId);
            Console.WriteLine("  ✓ Keycloak kullanıcıları institution_id ile güncellendi");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Keycloak güncelleme başarısız: {ex.Message}");
        }

        // Alanları aktifleştir
        await api.PostAsync($"/api/institutions/{institutionId}/branches", new { fieldCode = "BT" });
        Console.WriteLine("  ✓ Alan \"BT\" aktifleştirildi");

        await api.PostAsync($"/api/institutions/{institutionId}/branches", new { fieldCode = "MUF" });
        Console.WriteLine("  ✓ Alan \"MUF\" aktifleştirildi");

        // Personel ekle
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

        // Ders programı
        await api.PutAsync($"/api/institutions/{institutionId}/schedule-config", new
        {
            institutionId,
            dailyPeriodCount = 8,
            updatedBy = "Sistem"
        });
        Console.WriteLine("  ✓ Ders programı (8 ders) ayarlandı");

        // Akademik dönem
        await EnsureAcademicPeriodAsync(api, institutionId);
    }

    private static async Task EnsureAcademicPeriodAsync(MesnetApiClient api, Guid institutionId)
    {
        try
        {
            var periods = await api.GetAsync($"/api/institutions/{institutionId}/academic-periods");
            if (periods is { } pArr && pArr.ValueKind == System.Text.Json.JsonValueKind.Array && pArr.GetArrayLength() > 0)
            {
                Console.WriteLine("  → Akademik dönem mevcut, atlanıyor");
                return;
            }
        }
        catch { /* endpoint henüz yoksa veya hata varsa oluşturmayı dene */ }

        await api.PostAsync($"/api/institutions/{institutionId}/academic-periods", new
        {
            name = "2025-2026",
            startYear = 2025,
            endYear = 2026,
            startDate = "2025-09-08",
            endDate = "2026-06-19"
        });
        Console.WriteLine("  ✓ Akademik dönem \"2025-2026\" oluşturuldu");
    }
}
