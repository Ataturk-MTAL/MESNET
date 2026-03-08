namespace MESNET.Seeder.Seeders;

public static class CoordinationSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── Koordinasyon ───────────────────");

        if (!ctx.Has("Institution")) return;
        var institutionId = ctx.Get("Institution");

        await SeedGuidanceVisits(api, ctx, institutionId);
        await SeedBusinessEvaluation(api, ctx, institutionId);
        await SeedSkillExam(api, ctx, institutionId);
        await SeedActivityReport(api, ctx, institutionId);
    }

    private static async Task SeedGuidanceVisits(MesnetApiClient api, SeedContext ctx, Guid institutionId)
    {
        // Mevcut ziyaretleri yükle — GetAsync → envelope.Data = PagedResult { items: [...] }
        var existing = await api.GetAsync("/api/coordination/guidance-visits?pageSize=100");
        var existingKeys = new HashSet<string>(); // "teacherId|businessId"
        var existingList = new List<(string Key, Guid Id)>();
        if (existing is { } pagedResult
            && pagedResult.TryGetProperty("items", out var itemsEl)
            && itemsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                var tid = item.GetProperty("teacherId").GetGuid();
                var bid = item.GetProperty("businessId").GetGuid();
                var id = item.GetProperty("id").GetGuid();
                existingKeys.Add($"{tid}|{bid}");
                existingList.Add(($"{tid}|{bid}", id));
            }
        }

        // Guidance Visit 1: Approved
        if (ctx.Has("Teacher1") && ctx.Has("Business1") && ctx.Has("Student1"))
        {
            var matchKey = $"{ctx.Get("Teacher1")}|{ctx.Get("Business1")}";
            var found = existingList.FirstOrDefault(x => x.Key == matchKey);
            if (found.Id != Guid.Empty)
            {
                ctx.Set("Visit1", found.Id);
                Console.WriteLine("  → Rehberlik ziyareti 1 mevcut, yüklendi");
            }
            else
            {
                var d = await api.PostAsync("/api/coordination/guidance-visits", new
                {
                    teacherId = ctx.Get("Teacher1"),
                    businessId = ctx.Get("Business1"),
                    institutionId,
                    visitDate = DateTime.UtcNow.AddDays(-10),
                    studentNotes = new[]
                    {
                        new
                        {
                            studentId = ctx.Get("Student1"),
                            attendanceStatus = "Present",
                            workplaceAdaptation = "Good",
                            notes = "Öğrenci işyerine uyum sağlamış."
                        }
                    },
                    instructorMeetingNotes = "Usta öğretici ile görüşme yapıldı.",
                    issuesIdentified = (string?)null,
                    actionsTaken = (string?)null,
                    generalAssessment = "Staj süreci olumlu ilerliyor."
                });
                if (d is not null)
                {
                    var visitId = d.Value.GetProperty("visitId").GetGuid();
                    ctx.Set("Visit1", visitId);
                    await api.PostAsync($"/api/coordination/guidance-visits/{visitId}/submit");
                    await api.PostAsync($"/api/coordination/guidance-visits/{visitId}/approve");
                    Console.WriteLine("  ✓ Rehberlik ziyareti 1: oluşturuldu → sunuldu → onaylandı");
                }
            }
        }

        // Guidance Visit 2: Draft (BT öğretmen → BT işletme)
        if (ctx.Has("Teacher3") && ctx.Has("Business2") && ctx.Has("Student2"))
        {
            var matchKey = $"{ctx.Get("Teacher3")}|{ctx.Get("Business2")}";
            var found = existingList.FirstOrDefault(x => x.Key == matchKey);
            if (found.Id != Guid.Empty)
            {
                ctx.Set("Visit2", found.Id);
                Console.WriteLine("  → Rehberlik ziyareti 2 mevcut, yüklendi");
            }
            else
            {
                var d = await api.PostAsync("/api/coordination/guidance-visits", new
                {
                    teacherId = ctx.Get("Teacher3"),
                    businessId = ctx.Get("Business2"),
                    institutionId,
                    visitDate = DateTime.UtcNow.AddDays(-3),
                    studentNotes = new[]
                    {
                        new
                        {
                            studentId = ctx.Get("Student2"),
                            attendanceStatus = "Present",
                            workplaceAdaptation = "Fair",
                            notes = "Öğrencinin motivasyonu artırılmalı."
                        }
                    },
                    generalAssessment = "Genel değerlendirme henüz tamamlanmadı."
                });
                if (d is not null)
                {
                    ctx.Set("Visit2", d.Value.GetProperty("visitId").GetGuid());
                    Console.WriteLine("  ✓ Rehberlik ziyareti 2: taslak");
                }
            }
        }
    }

    private static async Task SeedBusinessEvaluation(MesnetApiClient api, SeedContext ctx, Guid institutionId)
    {
        if (!ctx.Has("Business1") || !ctx.Has("Teacher1")) return;

        // Mevcut değerlendirmeleri kontrol et — GetAsync → envelope.Data = PagedResult { items: [...] }
        var existing = await api.GetAsync($"/api/coordination/business-evaluations?businessId={ctx.Get("Business1")}&pageSize=10");
        if (existing is { } pagedResult1
            && pagedResult1.TryGetProperty("items", out var itemsEl1)
            && itemsEl1.ValueKind == System.Text.Json.JsonValueKind.Array
            && itemsEl1.GetArrayLength() > 0)
        {
            ctx.Set("Evaluation1", itemsEl1[0].GetProperty("id").GetGuid());
            Console.WriteLine("  → İşletme değerlendirme mevcut, yüklendi");
            return;
        }

        var d = await api.PostAsync("/api/coordination/business-evaluations", new
        {
            businessId = ctx.Get("Business1"),
            institutionId,
            evaluatorId = ctx.Get("Teacher1"),
            evaluationDate = DateTime.UtcNow.AddDays(-14),
            items = new[]
            {
                new { criterion = "İş Güvenliği",  score = 9, notes = "Uygun" },
                new { criterion = "Çalışma Ortamı", score = 8, notes = "İyi" },
                new { criterion = "Eğitim Desteği", score = 7, notes = "Yeterli" }
            },
            result = "Suitable",
            notes = "İşletme staj için uygundur."
        });
        if (d is not null)
        {
            ctx.Set("Evaluation1", d.Value.GetProperty("evaluationId").GetGuid());
            Console.WriteLine("  ✓ İşletme değerlendirme: Uygun");
        }
    }

    private static async Task SeedSkillExam(MesnetApiClient api, SeedContext ctx, Guid institutionId)
    {
        if (!ctx.Has("Student1") || !ctx.Has("Business1")) return;

        // Mevcut sınavları kontrol et — GetAsync → envelope.Data = PagedResult { items: [...] }
        var existing = await api.GetAsync($"/api/coordination/skill-exams?studentId={ctx.Get("Student1")}&academicYear=2025&semester=Spring&pageSize=10");
        if (existing is { } pagedResult2
            && pagedResult2.TryGetProperty("items", out var itemsEl2)
            && itemsEl2.ValueKind == System.Text.Json.JsonValueKind.Array
            && itemsEl2.GetArrayLength() > 0)
        {
            ctx.Set("Exam1", itemsEl2[0].GetProperty("id").GetGuid());
            Console.WriteLine("  → Beceri sınavı mevcut, yüklendi");
            return;
        }

        var d = await api.PostAsync("/api/coordination/skill-exams", new
        {
            studentId = ctx.Get("Student1"),
            businessId = ctx.Get("Business1"),
            institutionId,
            academicYear = 2025,
            semester = "Spring",
            examDate = DateTime.UtcNow.AddDays(-7),
            score = 82,
            criteria = new[]
            {
                new { name = "Teknik Beceri", score = 85, maxScore = 100 },
                new { name = "İş Disiplini",  score = 80, maxScore = 100 },
                new { name = "İletişim",       score = 78, maxScore = 100 }
            },
            committeeMembers = new[]
            {
                new { name = "Ayşe Çelik",  role = "Teacher" },
                new { name = "Mehmet Kaya", role = "BusinessRepresentative" }
            },
            result = "Passed"
        });
        if (d is not null)
        {
            ctx.Set("Exam1", d.Value.GetProperty("examId").GetGuid());
            Console.WriteLine("  ✓ Beceri sınavı: Başarılı (82 puan)");
        }
    }

    private static async Task SeedActivityReport(MesnetApiClient api, SeedContext ctx, Guid institutionId)
    {
        if (!ctx.Has("Student1") || !ctx.Has("Business1") || !ctx.Has("Teacher1")) return;

        // Mevcut raporları kontrol et — GetAsync → envelope.Data = PagedResult { items: [...] }
        var existing = await api.GetAsync($"/api/coordination/activity-reports?studentId={ctx.Get("Student1")}&year=2025&month=12&pageSize=10");
        if (existing is { } pagedResult3
            && pagedResult3.TryGetProperty("items", out var itemsEl3)
            && itemsEl3.ValueKind == System.Text.Json.JsonValueKind.Array
            && itemsEl3.GetArrayLength() > 0)
        {
            ctx.Set("Report1", itemsEl3[0].GetProperty("id").GetGuid());
            Console.WriteLine("  → Aylık faaliyet raporu mevcut, yüklendi");
            return;
        }

        var d = await api.PostAsync("/api/coordination/activity-reports", new
        {
            studentId = ctx.Get("Student1"),
            businessId = ctx.Get("Business1"),
            institutionId,
            teacherId = ctx.Get("Teacher1"),
            year = 2025,
            month = 12,
            activities = new[]
            {
                new { day = 1, description = "Elektrik tesisat planı inceleme", hours = 8 },
                new { day = 2, description = "Pano montaj çalışması",           hours = 8 },
                new { day = 3, description = "Kablo döşeme uygulaması",         hours = 8 },
                new { day = 4, description = "Topraklama ve izolasyon testi",   hours = 8 },
                new { day = 5, description = "İş güvenliği eğitimi",           hours = 4 }
            },
            instructorComment = "Öğrenci aktif katılım sağlıyor.",
            teacherComment = "Gelişim olumlu."
        });
        if (d is not null)
        {
            var reportId = d.Value.GetProperty("reportId").GetGuid();
            ctx.Set("Report1", reportId);
            await api.PostAsync($"/api/coordination/activity-reports/{reportId}/submit");
            Console.WriteLine("  ✓ Aylık faaliyet raporu: oluşturuldu → sunuldu");
        }
    }
}
