namespace MESNET.Seeder.Seeders;

public static class BusinessSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── İşletmeler ─────────────────────");

        if (!ctx.Has("Institution")) return;
        var tenantId = ctx.Get("Institution");

        // Mevcut işletmeleri yükle — GetAsync → envelope.Data = PagedResult { items: [...] }
        // pageSize=500 ile tüm işletmeleri tek istekte çek
        var existing = await api.GetAsync("/api/businesses?pageSize=500");
        var existingByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (existing is { } pagedResult
            && pagedResult.TryGetProperty("items", out var itemsEl)
            && itemsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                var name = item.GetProperty("name").GetString() ?? "";
                var id = item.GetProperty("id").GetGuid();
                existingByName[name] = id;
            }
        }

        Console.WriteLine($"  ℹ Mevcut işletme sayısı: {existingByName.Count}");

        // ── Orijinal 5 işletme (ctx key'leri korunuyor) ──────────────────
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

        // ── 95 ek işletme — Mersin genelinde gerçekçi dağılım ──────────────────
        var bulkBusinesses = GetBulkBusinessData();
        var createdCount = 0;

        foreach (var b in bulkBusinesses)
        {
            if (existingByName.ContainsKey(b.Name))
            {
                continue; // zaten var, atla
            }

            var data = await api.PostAsync("/api/businesses", new
            {
                tenantId,
                name = b.Name,
                address = b.Address,
                phoneNumber = b.Phone,
                email = b.Email,
                personnelCount = b.PersonnelCount,
                location = new { latitude = b.Lat, longitude = b.Lon },
                totalSlots = b.TotalSlots,
                sectors = b.Sectors
            });

            if (data is not null)
            {
                createdCount++;
                if (createdCount % 10 == 0)
                    Console.WriteLine($"  ✓ {createdCount} işletme oluşturuldu...");
            }
        }

        if (createdCount > 0)
            Console.WriteLine($"  ✓ Toplam {createdCount} yeni işletme oluşturuldu");
        else
            Console.WriteLine("  → Tüm işletmeler zaten mevcut");

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
        if (createdCount > 0)
        {
            Console.WriteLine("  … Enrollment event consumer bekleniyor (5s)...");
            await Task.Delay(TimeSpan.FromSeconds(5));
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

    // ── Toplu işletme veri listesi ──────────────────────────────────────────
    private static List<BulkBusinessData> GetBulkBusinessData()
    {
        // Mersin merkez 4 ilçe — koordinat aralıkları (gerçekçi):
        // Yenişehir:  36.790-36.810, 34.610-34.650
        // Mezitli:    36.770-36.790, 34.540-34.580
        // Akdeniz:    36.795-36.815, 34.600-34.640
        // Toroslar:   36.810-36.840, 34.610-34.650

        return
        [
            // ─── Yenişehir — Bilişim Kümesi (15 işletme) ────────────────
            new("Datamarin Yazılım A.Ş.", "Forum Mah., Yenişehir, Mersin", "0324 328 1001", "info@datamarin.com.tr", 36.7985, 34.6350, 30, 4, ["InformationTechnology"]),
            new("Netsis Mersin Bayi", "Limonluk Mah., Yenişehir, Mersin", "0324 328 1002", "mersin@netsis.com.tr", 36.7950, 34.6380, 15, 3, ["InformationTechnology"]),
            new("Kodlama Akademi Ltd. Şti.", "Akkent Mah., Yenişehir, Mersin", "0324 328 1003", "info@kodlamaakademi.com", 36.7972, 34.6410, 10, 2, ["InformationTechnology", "Education"]),
            new("Akıllı Çözümler Bilişim", "Bahçelievler Mah., Yenişehir, Mersin", "0324 328 1004", "info@akillicozumler.com", 36.8010, 34.6290, 8, 2, ["InformationTechnology"]),
            new("Mersin Web Tasarım", "Güvenevler Mah., Yenişehir, Mersin", "0324 328 1005", "info@mersinweb.com", 36.7998, 34.6445, 6, 2, ["InformationTechnology"]),
            new("Otomasyon Teknoloji A.Ş.", "Çiftlikköy Mah., Yenişehir, Mersin", "0324 328 1006", "info@ototek.com.tr", 36.8020, 34.6400, 22, 5, ["InformationTechnology", "ElectricalAndElectronics"]),
            new("Bulut Bilgi Sistemleri", "Menteş Mah., Yenişehir, Mersin", "0324 328 1007", "info@bulutbilgi.com", 36.7935, 34.6320, 12, 3, ["InformationTechnology"]),
            new("Dijital Dönüşüm Danışmanlık", "Palmiye Mah., Yenişehir, Mersin", "0324 328 1008", "info@dijitaldnm.com", 36.7960, 34.6470, 9, 2, ["InformationTechnology", "BusinessAndManagement"]),
            new("Siber Güvenlik Mersin", "Pozcu Mah., Yenişehir, Mersin", "0324 328 1009", "info@siberguvenlik33.com", 36.8008, 34.6180, 14, 3, ["InformationTechnology"]),
            new("GameDev Studio Mersin", "Fulay Mah., Yenişehir, Mersin", "0324 328 1010", "info@gamedevmersin.com", 36.7942, 34.6355, 7, 2, ["InformationTechnology", "CultureArtAndDesign"]),
            new("ERP Çözümleri Ltd. Şti.", "2. Çiftlik Mah., Yenişehir, Mersin", "0324 328 1011", "info@erpcozumleri.com", 36.8025, 34.6330, 16, 3, ["InformationTechnology"]),
            new("Yapay Zeka Laboratuvarı", "Barbaros Mah., Yenişehir, Mersin", "0324 328 1012", "info@yzlab.com.tr", 36.7990, 34.6260, 11, 3, ["InformationTechnology"]),
            new("Akdeniz Veri Merkezi", "Limonluk Mah., Yenişehir, Mersin", "0324 328 1013", "info@akdenizveri.com", 36.7955, 34.6395, 20, 4, ["InformationTechnology", "Energy"]),
            new("MersinSoft Yazılım", "Akkent Mah., Yenişehir, Mersin", "0324 328 1014", "info@mersinsoft.com", 36.7968, 34.6425, 13, 3, ["InformationTechnology"]),
            new("TeknoÇağ Bilişim", "Güvenevler Mah., Yenişehir, Mersin", "0324 328 1015", "info@teknocag.com.tr", 36.8002, 34.6455, 9, 2, ["InformationTechnology"]),

            // ─── Mezitli — Elektrik/Elektronik Kümesi (12 işletme) ──────
            new("Mezitli Elektrik Sanayi", "Davultepe Mah., Mezitli, Mersin", "0324 358 2001", "info@mezitlielektrik.com", 36.7750, 34.5520, 35, 6, ["ElectricalAndElectronics"]),
            new("Akdeniz Enerji Sistemleri", "Kuyuluk Mah., Mezitli, Mersin", "0324 358 2002", "info@akdenizenerji.com.tr", 36.7780, 34.5580, 28, 5, ["ElectricalAndElectronics", "Energy"]),
            new("Solar Panel Mersin", "Tece Mah., Mezitli, Mersin", "0324 358 2003", "info@solarpanelmersin.com", 36.7720, 34.5450, 18, 4, ["Energy", "ElectricalAndElectronics"]),
            new("Endüstriyel Kontrol Sistemleri", "Viranşehir Mah., Mezitli, Mersin", "0324 358 2004", "info@ekskontrol.com", 36.7810, 34.5600, 15, 3, ["ElectricalAndElectronics", "Machinery"]),
            new("Akıllı Ev Teknolojileri", "Fatih Mah., Mezitli, Mersin", "0324 358 2005", "info@akilliev33.com", 36.7795, 34.5560, 10, 2, ["ElectricalAndElectronics", "InformationTechnology"]),
            new("Güneş Enerjisi Mühendislik", "Davultepe Mah., Mezitli, Mersin", "0324 358 2006", "info@gunesenerjimhds.com", 36.7740, 34.5490, 22, 4, ["Energy"]),
            new("Elektro-Tek Mühendislik", "Kuyuluk Mah., Mezitli, Mersin", "0324 358 2007", "info@elektrotek33.com", 36.7770, 34.5540, 12, 3, ["ElectricalAndElectronics"]),
            new("Rüzgar Enerji A.Ş.", "Tece Mah., Mezitli, Mersin", "0324 358 2008", "info@ruzgarenerji.com.tr", 36.7735, 34.5470, 25, 4, ["Energy"]),
            new("LED Aydınlatma Mersin", "Viranşehir Mah., Mezitli, Mersin", "0324 358 2009", "info@ledmersin.com", 36.7805, 34.5615, 8, 2, ["ElectricalAndElectronics"]),
            new("Mekatronik Çözümler", "Fatih Mah., Mezitli, Mersin", "0324 358 2010", "info@mekatronik33.com", 36.7788, 34.5575, 14, 3, ["ElectricalAndElectronics", "Machinery"]),
            new("Asansör Teknik Mersin", "Davultepe Mah., Mezitli, Mersin", "0324 358 2011", "info@asansortek33.com", 36.7755, 34.5510, 20, 3, ["ElectricalAndElectronics", "Metal"]),
            new("Klima ve Soğutma Sistemleri", "Kuyuluk Mah., Mezitli, Mersin", "0324 358 2012", "info@klimamersin.com", 36.7775, 34.5550, 16, 3, ["Machinery"]),

            // ─── Akdeniz — Finans/Ticaret Kümesi (12 işletme) ───────────
            new("Çukurova Sigorta Acentesi", "Cami Şerif Mah., Akdeniz, Mersin", "0324 232 3001", "info@cukurovasigorta.com", 36.8000, 34.6200, 10, 2, ["Finance"]),
            new("Mersin Liman Lojistik A.Ş.", "Nusratiye Mah., Akdeniz, Mersin", "0324 232 3002", "info@mersinliman.com.tr", 36.7980, 34.6130, 45, 6, ["TransportLogisticsAndCommunication"]),
            new("Akdeniz Dış Ticaret", "Mahmudiye Mah., Akdeniz, Mersin", "0324 232 3003", "info@akdenizticaret.com.tr", 36.8015, 34.6170, 20, 4, ["Commerce"]),
            new("Güney Mali Müşavirlik", "Kuvay-ı Milliye Mah., Akdeniz, Mersin", "0324 232 3004", "info@guneymali.com", 36.8030, 34.6220, 8, 2, ["Finance", "BusinessAndManagement"]),
            new("Denizcilik ve Gümrük Hizmetleri", "İsmet İnönü Mah., Akdeniz, Mersin", "0324 232 3005", "info@denizgumruk.com", 36.7965, 34.6110, 15, 3, ["TransportLogisticsAndCommunication", "Commerce"]),
            new("Mersin Gümrük Müşavirliği", "Cami Şerif Mah., Akdeniz, Mersin", "0324 232 3006", "info@mersingumruk.com", 36.8005, 34.6185, 12, 3, ["Commerce", "Finance"]),
            new("İç Anadolu Nakliyat", "Nusratiye Mah., Akdeniz, Mersin", "0324 232 3007", "info@icanadolunak.com", 36.7975, 34.6145, 30, 4, ["TransportLogisticsAndCommunication"]),
            new("Uluslararası Freight Forwarding", "Mahmudiye Mah., Akdeniz, Mersin", "0324 232 3008", "info@uffmersin.com", 36.8010, 34.6155, 18, 3, ["TransportLogisticsAndCommunication", "Commerce"]),
            new("Mersin Serbest Bölge İşletmeleri", "Karaduvar Mah., Akdeniz, Mersin", "0324 232 3009", "info@mersinserbest.com", 36.8050, 34.6250, 55, 8, ["Commerce", "BusinessAndManagement"]),
            new("Akdeniz Vergi Danışmanlığı", "Kuvay-ı Milliye Mah., Akdeniz, Mersin", "0324 232 3010", "info@akdenizvergi.com", 36.8035, 34.6235, 7, 2, ["Finance"]),
            new("MerPort Depolama", "İsmet İnönü Mah., Akdeniz, Mersin", "0324 232 3011", "info@merport.com.tr", 36.7960, 34.6095, 40, 5, ["TransportLogisticsAndCommunication"]),
            new("Çukurova İthalat İhracat", "Cami Şerif Mah., Akdeniz, Mersin", "0324 232 3012", "info@cukurovaithrc.com", 36.7995, 34.6210, 14, 3, ["Commerce"]),

            // ─── Toroslar — Makine/Metal Kümesi (12 işletme) ────────────
            new("Toroslar Makine Sanayi", "Arpaçsakarlar Mah., Toroslar, Mersin", "0324 342 4001", "info@toroslarmakinasanayi.com", 36.8150, 34.6200, 40, 6, ["Machinery", "Metal"]),
            new("Çelik Konstrüksiyon Mersin", "Yalınayak Mah., Toroslar, Mersin", "0324 342 4002", "info@celikkons33.com", 36.8200, 34.6250, 35, 5, ["Metal", "Construction"]),
            new("CNC Hassas İşleme", "Alsancak Mah., Toroslar, Mersin", "0324 342 4003", "info@cncmersin.com", 36.8180, 34.6300, 18, 3, ["Machinery"]),
            new("Mersin Döküm Sanayi", "Arpaçsakarlar Mah., Toroslar, Mersin", "0324 342 4004", "info@mersindokum.com", 36.8160, 34.6220, 50, 6, ["Metal"]),
            new("Hidrolik Pnömatik Sistemler", "Yalınayak Mah., Toroslar, Mersin", "0324 342 4005", "info@hidropno33.com", 36.8210, 34.6270, 15, 3, ["Machinery"]),
            new("Kaynak ve Metal İşleri", "Alsancak Mah., Toroslar, Mersin", "0324 342 4006", "info@kaynakmersin.com", 36.8175, 34.6310, 12, 2, ["Metal"]),
            new("Otomotiv Yedek Parça Mersin", "Güneykent Mah., Toroslar, Mersin", "0324 342 4007", "info@otoyedekmersin.com", 36.8130, 34.6280, 20, 4, ["Automotive"]),
            new("Torna ve Freze Atölyesi", "Arpaçsakarlar Mah., Toroslar, Mersin", "0324 342 4008", "info@tornafreze33.com", 36.8145, 34.6235, 8, 2, ["Machinery"]),
            new("Alüminyum Profil A.Ş.", "Yalınayak Mah., Toroslar, Mersin", "0324 342 4009", "info@aluprofil33.com", 36.8195, 34.6260, 25, 4, ["Metal"]),
            new("Plastik Kalıp Sanayi", "Alsancak Mah., Toroslar, Mersin", "0324 342 4010", "info@plastikkalip33.com", 36.8185, 34.6320, 22, 4, ["ChemistryPetroleumRubberPlastics"]),
            new("Endüstriyel Boya ve Kaplama", "Güneykent Mah., Toroslar, Mersin", "0324 342 4011", "info@endboya33.com", 36.8125, 34.6295, 14, 3, ["ChemistryPetroleumRubberPlastics", "Metal"]),
            new("Mersin Oto Tamir ve Bakım", "Arpaçsakarlar Mah., Toroslar, Mersin", "0324 342 4012", "info@mersinototamir.com", 36.8155, 34.6215, 10, 3, ["Automotive"]),

            // ─── Akdeniz — Gıda/Ticaret Kümesi 2 (12 işletme) ────────────
            new("Mersin Gıda Sanayi A.Ş.", "Nusratiye Mah., Akdeniz, Mersin", "0324 232 5001", "info@mersingida.com.tr", 36.7988, 34.6100, 60, 8, ["Food", "AgricultureHuntingAndFishing"]),
            new("Berdan Un Fabrikası", "Cami Şerif Mah., Akdeniz, Mersin", "0324 232 5002", "info@berdanun.com", 36.8008, 34.6175, 35, 5, ["Food"]),
            new("Çukurova Tarım İlaçları", "Mahmudiye Mah., Akdeniz, Mersin", "0324 232 5003", "info@cukurovatim.com", 36.8020, 34.6140, 25, 4, ["AgricultureHuntingAndFishing", "ChemistryPetroleumRubberPlastics"]),
            new("Mersin Zeytin ve Zeytinyağı", "İsmet İnönü Mah., Akdeniz, Mersin", "0324 232 5004", "info@mersinzeytin.com", 36.7970, 34.6120, 18, 3, ["Food"]),
            new("Narenciye Paketleme A.Ş.", "Nusratiye Mah., Akdeniz, Mersin", "0324 232 5005", "info@narenciye33.com", 36.7985, 34.6085, 30, 5, ["Food", "AgricultureHuntingAndFishing"]),
            new("Sera Teknolojileri Mersin", "Karaduvar Mah., Akdeniz, Mersin", "0324 232 5006", "info@seratek33.com", 36.8055, 34.6260, 12, 3, ["AgricultureHuntingAndFishing"]),
            new("Akdeniz Süt Ürünleri", "Cami Şerif Mah., Akdeniz, Mersin", "0324 232 5007", "info@akdenizsut.com", 36.8012, 34.6190, 20, 4, ["Food"]),
            new("Baharat ve Bakliyat Toptancısı", "Mahmudiye Mah., Akdeniz, Mersin", "0324 232 5008", "info@baharatmersin.com", 36.8025, 34.6160, 8, 2, ["Food", "Commerce"]),
            new("Organik Çiftlik Ürünleri", "İsmet İnönü Mah., Akdeniz, Mersin", "0324 232 5009", "info@organik33.com", 36.7972, 34.6105, 15, 3, ["AgricultureHuntingAndFishing", "Food"]),
            new("Mersin Soğuk Hava Depoları", "Karaduvar Mah., Akdeniz, Mersin", "0324 232 5010", "info@sogukdepo33.com", 36.8045, 34.6275, 40, 5, ["Food", "TransportLogisticsAndCommunication"]),
            new("Gübre ve Tohum Dağıtım", "Nusratiye Mah., Akdeniz, Mersin", "0324 232 5011", "info@gubredagitim.com", 36.7992, 34.6115, 10, 2, ["AgricultureHuntingAndFishing"]),
            new("Çiçekçilik ve Peyzaj Mersin", "Cami Şerif Mah., Akdeniz, Mersin", "0324 232 5012", "info@cicekmersin.com", 36.8002, 34.6200, 6, 2, ["AgricultureHuntingAndFishing", "CommunityAndPersonalServices"]),

            // ─── Toroslar — İnşaat/Yapı Kümesi (10 işletme) ────────────
            new("Toroslar İnşaat A.Ş.", "Yalınayak Mah., Toroslar, Mersin", "0324 342 6001", "info@toroslarinsaat.com", 36.8220, 34.6200, 45, 6, ["Construction"]),
            new("Akdeniz Beton Sanayi", "Alsancak Mah., Toroslar, Mersin", "0324 342 6002", "info@akdenizbeton.com", 36.8190, 34.6340, 30, 4, ["Construction", "Mining"]),
            new("Yenişehir Turizm Otelcilik", "Bahçelievler Mah., Yenişehir, Mersin", "0324 328 6003", "info@yenisehirturizm.com", 36.8008, 34.6310, 25, 5, ["TourismHospitalityAndFoodServices"]),
            new("Marina Otel ve Konaklama", "Pozcu Mah., Yenişehir, Mersin", "0324 328 6004", "info@marinaotel33.com", 36.8000, 34.6175, 50, 6, ["TourismHospitalityAndFoodServices"]),
            new("Mersin Mermer ve Doğaltaş", "Arpaçsakarlar Mah., Toroslar, Mersin", "0324 342 6005", "info@mersinmermer.com", 36.8165, 34.6250, 20, 3, ["Mining", "Construction"]),
            new("Toroslar Hazır Beton", "Güneykent Mah., Toroslar, Mersin", "0324 342 6006", "info@toroslarhazbeton.com", 36.8135, 34.6230, 15, 3, ["Construction"]),
            new("Sahil Restoran ve Konaklama", "Güvenevler Mah., Yenişehir, Mersin", "0324 328 6007", "info@sahilmersin.com", 36.7995, 34.6460, 18, 4, ["TourismHospitalityAndFoodServices"]),
            new("Demir Çelik Yapı Malzemeleri", "Alsancak Mah., Toroslar, Mersin", "0324 342 6008", "info@demircelik33.com", 36.8185, 34.6330, 22, 3, ["Construction", "Metal"]),
            new("Toroslar Peyzaj ve Çevre", "Yalınayak Mah., Toroslar, Mersin", "0324 342 6009", "info@toroslar-peyzaj.com", 36.8215, 34.6215, 10, 2, ["Environment", "Construction"]),
            new("Yapı Malzemeleri Deposu", "Güneykent Mah., Toroslar, Mersin", "0324 342 6010", "info@yapimersin.com", 36.8140, 34.6245, 28, 4, ["Construction", "GlassCementAndSoil"]),

            // ─── Yenişehir/Mezitli — Sağlık Kümesi (7 işletme) ──────────
            new("Mersin Özel Tıp Merkezi", "Forum Mah., Yenişehir, Mersin", "0324 328 8001", "info@mersinotm.com", 36.7980, 34.6300, 80, 8, ["HealthAndSocialServices"]),
            new("Akdeniz Eczane Deposu", "Palmiye Mah., Yenişehir, Mersin", "0324 328 8002", "info@akdenizeczane.com", 36.7958, 34.6340, 20, 3, ["HealthAndSocialServices"]),
            new("Diş Protez Laboratuvarı", "Güvenevler Mah., Yenişehir, Mersin", "0324 328 8003", "info@disprotez33.com", 36.8015, 34.6270, 10, 2, ["HealthAndSocialServices"]),
            new("Medikal Cihaz Mersin", "Pozcu Mah., Yenişehir, Mersin", "0324 328 8004", "info@medikalmersin.com", 36.8005, 34.6195, 15, 3, ["HealthAndSocialServices", "ElectricalAndElectronics"]),
            new("Veteriner Kliniği ve Pet Shop", "Limonluk Mah., Yenişehir, Mersin", "0324 328 8005", "info@vetmersin.com", 36.7945, 34.6370, 8, 2, ["HealthAndSocialServices"]),
            new("Optik ve Lens Merkezi", "Bahçelievler Mah., Yenişehir, Mersin", "0324 328 8006", "info@optik33.com", 36.8012, 34.6285, 6, 2, ["HealthAndSocialServices"]),
            new("Fizik Tedavi ve Rehabilitasyon", "Akkent Mah., Yenişehir, Mersin", "0324 328 8007", "info@fizikmersin.com", 36.7975, 34.6415, 12, 3, ["HealthAndSocialServices"]),

            // ─── Mezitli/Yenişehir — Tekstil/Medya/Spor/Diğer (14 işletme) ────
            new("Mersin Tekstil Fabrikası", "Davultepe Mah., Mezitli, Mersin", "0324 358 9001", "info@mersintekstil.com.tr", 36.7745, 34.5500, 120, 10, ["TextileApparelAndLeather"]),
            new("Akdeniz Medya ve Yayıncılık", "Forum Mah., Yenişehir, Mersin", "0324 328 9002", "info@akdenizmedya.com", 36.7988, 34.6360, 14, 3, ["MediaCommunicationAndPublishing"]),
            new("Mersin Spor Kompleksi", "Çiftlikköy Mah., Yenişehir, Mersin", "0324 328 9003", "info@mersinspor33.com", 36.8018, 34.6430, 25, 4, ["SportsAndRecreation"]),
            new("Güzel Sanatlar Atölyesi", "Palmiye Mah., Yenişehir, Mersin", "0324 328 9004", "info@sanatmersin.com", 36.7952, 34.6480, 8, 2, ["CultureArtAndDesign"]),
            new("Çevre Mühendislik ve Arıtma", "Kazanlı Mah., Akdeniz, Mersin", "0324 232 9005", "info@cevrearitmamersin.com", 36.8060, 34.6270, 16, 3, ["Environment"]),
            new("Adalet ve Hukuk Danışmanlığı", "Kuvay-ı Milliye Mah., Akdeniz, Mersin", "0324 232 9006", "info@adaletmersin.com", 36.8040, 34.6240, 10, 2, ["JusticeAndSecurity"]),
            new("Cam ve Ayna Sanayi", "Arpaçsakarlar Mah., Toroslar, Mersin", "0324 342 9007", "info@cammersin.com", 36.8170, 34.6240, 18, 3, ["GlassCementAndSoil"]),
            new("Akdeniz Konfeksiyon", "Tece Mah., Mezitli, Mersin", "0324 358 9008", "info@akdenizkonfeksiyon.com", 36.7730, 34.5460, 35, 5, ["TextileApparelAndLeather"]),
            new("Mersin Balıkçılık ve Su Ürünleri", "Nusratiye Mah., Akdeniz, Mersin", "0324 232 9009", "info@mersinbalik.com", 36.7978, 34.6090, 15, 3, ["AgricultureHuntingAndFishing", "Food"]),
            new("Deniz Taşımacılığı Mersin", "İsmet İnönü Mah., Akdeniz, Mersin", "0324 232 9010", "info@deniztasima33.com", 36.7955, 34.6080, 25, 4, ["TransportLogisticsAndCommunication"]),
            new("Toroslar Arıcılık ve Bal", "Güneykent Mah., Toroslar, Mersin", "0324 342 9011", "info@toroslarbal.com", 36.8120, 34.6310, 6, 2, ["AgricultureHuntingAndFishing", "Food"]),
            new("Turizm ve Seyahat Acentesi", "Palmiye Mah., Yenişehir, Mersin", "0324 328 9012", "info@seyahat33.com", 36.7965, 34.6490, 12, 2, ["TourismHospitalityAndFoodServices"]),
            new("Matbaa ve Baskı Hizmetleri", "Akkent Mah., Yenişehir, Mersin", "0324 328 9013", "info@matbaa33.com", 36.7970, 34.6400, 10, 2, ["MediaCommunicationAndPublishing"]),
            new("Kuaför ve Güzellik Merkezi", "Fatih Mah., Mezitli, Mersin", "0324 358 9014", "info@guzellik33.com", 36.7800, 34.5590, 5, 2, ["CommunityAndPersonalServices"]),
            new("Spor Salonu ve Fitness", "Viranşehir Mah., Mezitli, Mersin", "0324 358 9015", "info@fitness33.com", 36.7815, 34.5625, 10, 2, ["SportsAndRecreation"]),
        ];
    }

    private sealed record BulkBusinessData(
        string Name,
        string Address,
        string Phone,
        string Email,
        double Lat,
        double Lon,
        int PersonnelCount,
        int TotalSlots,
        string[] Sectors);
}
