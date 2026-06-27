namespace MESNET.Seeder.Seeders;

public static class AttendanceSeeder
{
    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── Devamsızlık ────────────────────");

        if (!ctx.Has("Institution")) return;
        var institutionId = ctx.Get("Institution");
        var now = DateTime.UtcNow;

        // MarkAttendance yalnızca GEÇERLİ HAFTA tarihlerini kabul eder (MEB e-Okul uyumu) ve
        // academicPeriodId zorunludur. Bu haftanın Pazartesi'sini baz alıp gün ofsetiyle tarih üret.
        var monday = now.Date.AddDays(-(((int)now.DayOfWeek + 6) % 7));
        var periodId = ctx.Has("AcademicPeriod") ? ctx.Get("AcademicPeriod") : Guid.Empty;

        // Mevcut devamsızlık kayıtlarını yükle — GetAsync → envelope.Data = PagedResult { items: [...] }
        var existing = await api.GetAsync($"/api/attendance?institutionId={institutionId}&pageSize=100");
        var existingKeys = new Dictionary<string, Guid>(); // "studentId|date" → attendanceId
        if (existing is { } pagedResult
            && pagedResult.TryGetProperty("items", out var itemsEl)
            && itemsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in itemsEl.EnumerateArray())
            {
                var sid = item.GetProperty("studentId").GetGuid();
                var dateStr = item.GetProperty("date").GetString() ?? "";
                // Sadece tarih kısmını al (yyyy-MM-dd)
                var dateKey = dateStr.Length >= 10 ? dateStr[..10] : dateStr;
                var attendanceId = item.GetProperty("id").GetGuid();
                existingKeys[$"{sid}|{dateKey}"] = attendanceId;
            }
        }

        string DateKey(Guid studentId, DateTime date) =>
            $"{studentId}|{date:yyyy-MM-dd}";

        // Attendance 1: Mazeretsiz — doğrulanmış
        if (ctx.Has("Student1") && ctx.Has("Business1"))
        {
            var date = monday.AddDays(0);
            var key = DateKey(ctx.Get("Student1"), date);
            if (existingKeys.TryGetValue(key, out var eid))
            {
                ctx.Set("Attendance1", eid);
                Console.WriteLine("  → Devamsızlık 1 mevcut, yüklendi");
            }
            else
            {
                var d = await api.PostAsync("/api/attendance", new
                {
                    studentId = ctx.Get("Student1"),
                    businessId = ctx.Get("Business1"),
                    institutionId,
                    academicPeriodId = periodId,
                    date,
                    absenceType = "Unexcused",
                    markedBy = "Ayşe Çelik"
                });
                if (d is not null)
                {
                    var id = d.Value.GetProperty("attendanceId").GetGuid();
                    ctx.Set("Attendance1", id);
                    await api.PostAsync($"/api/attendance/{id}/verify", new { attendanceId = id, verifiedBy = "Ayşe Çelik" });
                    Console.WriteLine("  ✓ Devamsızlık 1: Mazeretsiz (doğrulanmış)");
                }
            }
        }

        // Attendance 2: Mazeretli — doğrulanmış
        if (ctx.Has("Student1") && ctx.Has("Business1"))
        {
            var date = monday.AddDays(1);
            var key = DateKey(ctx.Get("Student1"), date);
            if (existingKeys.TryGetValue(key, out var eid))
            {
                ctx.Set("Attendance2", eid);
                Console.WriteLine("  → Devamsızlık 2 mevcut, yüklendi");
            }
            else
            {
                var d = await api.PostAsync("/api/attendance", new
                {
                    studentId = ctx.Get("Student1"),
                    businessId = ctx.Get("Business1"),
                    institutionId,
                    academicPeriodId = periodId,
                    date,
                    absenceType = "Excused",
                    reason = "Aile izni",
                    markedBy = "Ayşe Çelik"
                });
                if (d is not null)
                {
                    var id = d.Value.GetProperty("attendanceId").GetGuid();
                    ctx.Set("Attendance2", id);
                    await api.PostAsync($"/api/attendance/{id}/verify", new { attendanceId = id, verifiedBy = "Ayşe Çelik" });
                    Console.WriteLine("  ✓ Devamsızlık 2: Mazeretli (doğrulanmış)");
                }
            }
        }

        // Attendance 3: Sağlık raporu — doğrulanmış (BT öğrenci)
        if (ctx.Has("Student2") && ctx.Has("Business2"))
        {
            var date = monday.AddDays(2);
            var key = DateKey(ctx.Get("Student2"), date);
            if (existingKeys.TryGetValue(key, out var eid))
            {
                ctx.Set("Attendance3", eid);
                Console.WriteLine("  → Devamsızlık 3 mevcut, yüklendi");
            }
            else
            {
                var d = await api.PostAsync("/api/attendance", new
                {
                    studentId = ctx.Get("Student2"),
                    businessId = ctx.Get("Business2"),
                    institutionId,
                    academicPeriodId = periodId,
                    date,
                    absenceType = "HealthReport",
                    reason = "Sağlık raporu",
                    markedBy = "Hasan Kara"
                });
                if (d is not null)
                {
                    var id = d.Value.GetProperty("attendanceId").GetGuid();
                    ctx.Set("Attendance3", id);
                    await api.PostAsync($"/api/attendance/{id}/verify", new { attendanceId = id, verifiedBy = "Hasan Kara" });
                    Console.WriteLine("  ✓ Devamsızlık 3: Sağlık raporu (doğrulanmış)");
                }
            }
        }

        // Attendance 4: Mazeretsiz — doğrulanmamış (BT öğrenci)
        if (ctx.Has("Student2") && ctx.Has("Business2"))
        {
            var date = monday.AddDays(3);
            var key = DateKey(ctx.Get("Student2"), date);
            if (existingKeys.TryGetValue(key, out var eid))
            {
                ctx.Set("Attendance4", eid);
                Console.WriteLine("  → Devamsızlık 4 mevcut, yüklendi");
            }
            else
            {
                var d = await api.PostAsync("/api/attendance", new
                {
                    studentId = ctx.Get("Student2"),
                    businessId = ctx.Get("Business2"),
                    institutionId,
                    academicPeriodId = periodId,
                    date,
                    absenceType = "Unexcused",
                    markedBy = "Hasan Kara"
                });
                if (d is not null)
                {
                    ctx.Set("Attendance4", d.Value.GetProperty("attendanceId").GetGuid());
                    Console.WriteLine("  ✓ Devamsızlık 4: Mazeretsiz (doğrulanmamış)");
                }
            }
        }

        // Attendance 5: Mazeretsiz → düzeltildi → mazeretli
        if (ctx.Has("Student1") && ctx.Has("Business1"))
        {
            var date = monday.AddDays(4);
            var key = DateKey(ctx.Get("Student1"), date);
            if (existingKeys.TryGetValue(key, out var eid))
            {
                ctx.Set("Attendance5", eid);
                Console.WriteLine("  → Devamsızlık 5 mevcut, yüklendi");
            }
            else
            {
                var d = await api.PostAsync("/api/attendance", new
                {
                    studentId = ctx.Get("Student1"),
                    businessId = ctx.Get("Business1"),
                    institutionId,
                    academicPeriodId = periodId,
                    date,
                    absenceType = "Unexcused",
                    markedBy = "Ayşe Çelik"
                });
                if (d is not null)
                {
                    var id = d.Value.GetProperty("attendanceId").GetGuid();
                    ctx.Set("Attendance5", id);
                    await api.PostAsync($"/api/attendance/{id}/correct", new
                    {
                        attendanceId = id,
                        newAbsenceType = "Excused",
                        reason = "Aile izni belgesi sunuldu",
                        correctedBy = "Ayşe Çelik"
                    });
                    Console.WriteLine("  ✓ Devamsızlık 5: Mazeretsiz → Mazeretli (düzeltildi)");
                }
            }
        }

        // Attendance 6: Mazeretsiz — kaydedilmiş
        if (ctx.Has("Student1") && ctx.Has("Business1"))
        {
            var date = monday.AddDays(0);
            var key = DateKey(ctx.Get("Student1"), date);
            if (existingKeys.TryGetValue(key, out var eid))
            {
                ctx.Set("Attendance6", eid);
                Console.WriteLine("  → Devamsızlık 6 mevcut, yüklendi");
            }
            else
            {
                var d = await api.PostAsync("/api/attendance", new
                {
                    studentId = ctx.Get("Student1"),
                    businessId = ctx.Get("Business1"),
                    institutionId,
                    academicPeriodId = periodId,
                    date,
                    absenceType = "Unexcused",
                    markedBy = "Ayşe Çelik"
                });
                if (d is not null)
                {
                    ctx.Set("Attendance6", d.Value.GetProperty("attendanceId").GetGuid());
                    Console.WriteLine("  ✓ Devamsızlık 6: Mazeretsiz (kaydedilmiş)");
                }
            }
        }
    }
}
