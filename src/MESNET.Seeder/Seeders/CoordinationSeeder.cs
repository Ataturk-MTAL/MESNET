namespace MESNET.Seeder.Seeders;

public static class CoordinationSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── Koordinasyon ───────────────────");

        if (!ctx.Has("Institution")) return;
        var institutionId = ctx.Get("Institution");

        // Guidance Visit 1: Approved
        if (ctx.Has("Teacher1") && ctx.Has("Business1") && ctx.Has("Student1"))
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

        // Guidance Visit 2: Draft
        if (ctx.Has("Teacher2") && ctx.Has("Business2") && ctx.Has("Student2"))
        {
            var d = await api.PostAsync("/api/coordination/guidance-visits", new
            {
                teacherId = ctx.Get("Teacher2"),
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

        // Business Evaluation: Suitable
        if (ctx.Has("Business1") && ctx.Has("Teacher1"))
        {
            var d = await api.PostAsync("/api/coordination/business-evaluations", new
            {
                businessId = ctx.Get("Business1"),
                institutionId,
                evaluatorId = ctx.Get("Teacher1"),
                evaluationDate = DateTime.UtcNow.AddDays(-14),
                items = new[]
                {
                    new { criterion = "İş Güvenliği", score = 9, notes = "Uygun" },
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

        // Skill Exam: Passed
        if (ctx.Has("Student1") && ctx.Has("Business1"))
        {
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
                    new { name = "İş Disiplini", score = 80, maxScore = 100 },
                    new { name = "İletişim", score = 78, maxScore = 100 }
                },
                committeeMembers = new[]
                {
                    new { name = "Ayşe Çelik", role = "Teacher" },
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

        // Monthly Activity Report: Submitted
        if (ctx.Has("Student1") && ctx.Has("Business1") && ctx.Has("Teacher1"))
        {
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
                    new { day = 1, description = "Yazılım geliştirme ortamı kurulumu", hours = 8 },
                    new { day = 2, description = "Veritabanı tasarımı çalışması", hours = 8 },
                    new { day = 3, description = "REST API geliştirme", hours = 8 },
                    new { day = 4, description = "Unit test yazma", hours = 8 },
                    new { day = 5, description = "Kod inceleme toplantısı", hours = 4 }
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
}
