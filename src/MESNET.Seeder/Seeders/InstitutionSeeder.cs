namespace MESNET.Seeder.Seeders;

public static class InstitutionSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx, KeycloakAdminService keycloak)
    {
        Console.WriteLine();
        Console.WriteLine("── Kurum ──────────────────────────");

        var institutionId = await EnsureInstitutionAsync(api);
        if (institutionId is null) return;

        ctx.Set("Institution", institutionId.Value);

        // Keycloak kullanıcılarının institution_id claim'ini güncelle
        await SyncKeycloakAsync(keycloak, institutionId.Value);

        // Alanlar + dallar
        await EnsureBranchesAsync(api, institutionId.Value);

        // Staff — Keycloak'tan gerçek kullanıcı ID'lerini al
        await EnsureStaffAsync(api, keycloak, institutionId.Value);

        // Ders programı
        await api.PutAsync($"/api/institutions/{institutionId.Value}/schedule-config", new
        {
            institutionId = institutionId.Value,
            dailyPeriodCount = 8,
            updatedBy = "Sistem"
        });
        Console.WriteLine("  ✓ Ders programı (8 ders) ayarlandı");

        // Akademik dönem
        await EnsureAcademicPeriodAsync(api, ctx, institutionId.Value);
    }

    private static async Task<Guid?> EnsureInstitutionAsync(MesnetApiClient api)
    {
        var existing = await api.GetAsync("/api/institutions");
        if (existing is { } arr && arr.ValueKind == System.Text.Json.JsonValueKind.Array && arr.GetArrayLength() > 0)
        {
            var id = arr[0].GetProperty("id").GetGuid();
            Console.WriteLine($"  → Kurum mevcut (id: {id.ToString()[..8]}...)");
            return id;
        }

        var data = await api.PostAsync("/api/institutions", new
        {
            tenantId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            institutionCode = 967523,
            fullName = "Atatürk Mesleki ve Teknik Anadolu Lisesi",
            address = "Toroslar, Mersin",
            phoneNumber = "0324 555 0001",
            email = "mersinataturk.mtal@meb.gov.tr",
            webUrl = "https://mersinataturkmtal.meb.k12.tr",
            location = new { latitude = 36.7956, longitude = 34.6119 }
        });

        if (data is null) return null;
        var institutionId = data.Value.GetProperty("id").GetGuid();
        Console.WriteLine($"  ✓ Kurum oluşturuldu (id: {institutionId.ToString()[..8]}...)");
        return institutionId;
    }

    private static async Task SyncKeycloakAsync(KeycloakAdminService keycloak, Guid institutionId)
    {
        try
        {
            await keycloak.UpdateAllUsersInstitutionIdAsync(institutionId);
            Console.WriteLine("  ✓ Keycloak institution_id senkronize edildi");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Keycloak senkronizasyon başarısız: {ex.Message}");
        }
    }

    private static async Task EnsureBranchesAsync(MesnetApiClient api, Guid institutionId)
    {
        // Branch aktifleştirme idempotent değil (zaten varsa 422 döner, sorun yok)
        // Specialization güncelleme idempotent (PUT — üzerine yazar)
        var branches = new[]
        {
            (Code: "EET", Specs: new[] { "EET-ETD", "EET-EBO" }, Label: "EET (Elektrik Tesisatları, Endüstriyel Bakım Onarım)"),
            (Code: "BT", Specs: new[] { "BT-AG", "BT-YAZ" }, Label: "BT (Ağ İşletmenliği, Yazılım Geliştirme)"),
            (Code: "MTT", Specs: new[] { "MTT-BMI", "MTT-MBO" }, Label: "MTT (Bilgisayarlı Makine İmalatı, Makine Bakım Onarım)"),
        };

        foreach (var (code, specs, label) in branches)
        {
            await api.PostAsync($"/api/institutions/{institutionId}/branches", new { fieldCode = code });
            await api.PutAsync($"/api/institutions/{institutionId}/branches/{code}/specializations",
                new { activeSpecializations = specs });
            Console.WriteLine($"  ✓ Alan \"{label}\" aktif");
        }
    }

    private static async Task EnsureStaffAsync(MesnetApiClient api, KeycloakAdminService keycloak, Guid institutionId)
    {
        // Mevcut staff'ı kontrol et
        var inst = await api.GetAsync($"/api/institutions/{institutionId}");
        var existingStaffIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (inst is { } instEl)
        {
            try
            {
                var staffArr = instEl.GetProperty("staff");
                foreach (var s in staffArr.EnumerateArray())
                    existingStaffIds.Add(s.GetProperty("keycloakId").GetString() ?? "");
            }
            catch { /* staff property yoksa boş set */ }
        }

        // username → (role, branchCode) eşleştirmesi
        var staffMapping = new Dictionary<string, (string Role, string? BranchCode)>
        {
            ["admin"] = ("Principal", null),
            ["teacher1"] = ("Coordinator", "EET"),
        };

        try
        {
            var kcUsers = await keycloak.GetRealmUsersAsync();

            foreach (var (username, (role, branchCode)) in staffMapping)
            {
                var kcUser = kcUsers.Find(u => u.Username == username);
                if (kcUser is null)
                {
                    Console.WriteLine($"  ⚠ Keycloak kullanıcısı \"{username}\" bulunamadı, atlanıyor");
                    continue;
                }

                if (existingStaffIds.Contains(kcUser.Id))
                {
                    Console.WriteLine($"  → Personel \"{kcUser.FullName}\" zaten atanmış");
                    continue;
                }

                await api.PostAsync($"/api/institutions/{institutionId}/staff", new
                {
                    keycloakId = kcUser.Id,
                    fullName = kcUser.FullName,
                    role,
                    branchCode
                });
                Console.WriteLine($"  ✓ Personel \"{kcUser.FullName}\" ({role}) eklendi [kc: {username}]");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ Keycloak staff ataması başarısız: {ex.Message}");
        }
    }

    private static async Task EnsureAcademicPeriodAsync(MesnetApiClient api, SeedContext ctx, Guid institutionId)
    {
        try
        {
            var periods = await api.GetAsync($"/api/institutions/{institutionId}/academic-periods");
            if (periods is { } pArr && pArr.ValueKind == System.Text.Json.JsonValueKind.Array && pArr.GetArrayLength() > 0)
            {
                var periodId = pArr[0].GetProperty("id").GetGuid();
                ctx.Set("AcademicPeriod", periodId);
                Console.WriteLine($"  → Akademik dönem mevcut (id: {periodId.ToString()[..8]}...)");
                return;
            }
        }
        catch { /* endpoint henüz yoksa veya hata varsa oluşturmayı dene */ }

        var data = await api.PostAsync($"/api/institutions/{institutionId}/academic-periods", new
        {
            name = "2025-2026",
            startYear = 2025,
            endYear = 2026,
            startDate = "2025-09-08",
            endDate = "2026-06-19"
        });
        if (data is not null)
        {
            var periodId = data.Value.GetProperty("id").GetGuid();
            ctx.Set("AcademicPeriod", periodId);
        }
        Console.WriteLine("  ✓ Akademik dönem \"2025-2026\" oluşturuldu");
    }
}
