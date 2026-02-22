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

        // Attendance 1: Mazeretsiz — doğrulanmış
        if (ctx.Has("Student1") && ctx.Has("Business1"))
        {
            var d = await api.PostAsync("/api/attendance", new
            {
                studentId = ctx.Get("Student1"),
                businessId = ctx.Get("Business1"),
                institutionId,
                date = now.AddDays(-15),
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

        // Attendance 2: Mazeretli — doğrulanmış
        if (ctx.Has("Student1") && ctx.Has("Business1"))
        {
            var d = await api.PostAsync("/api/attendance", new
            {
                studentId = ctx.Get("Student1"),
                businessId = ctx.Get("Business1"),
                institutionId,
                date = now.AddDays(-8),
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

        // Attendance 3: Sağlık raporu — doğrulanmış
        if (ctx.Has("Student2") && ctx.Has("Business2"))
        {
            var d = await api.PostAsync("/api/attendance", new
            {
                studentId = ctx.Get("Student2"),
                businessId = ctx.Get("Business2"),
                institutionId,
                date = now.AddDays(-12),
                absenceType = "HealthReport",
                reason = "Sağlık raporu",
                markedBy = "Mustafa Yılmaz"
            });
            if (d is not null)
            {
                var id = d.Value.GetProperty("attendanceId").GetGuid();
                ctx.Set("Attendance3", id);
                await api.PostAsync($"/api/attendance/{id}/verify", new { attendanceId = id, verifiedBy = "Mustafa Yılmaz" });
                Console.WriteLine("  ✓ Devamsızlık 3: Sağlık raporu (doğrulanmış)");
            }
        }

        // Attendance 4: Mazeretsiz — kaydedilmiş (doğrulanmamış)
        if (ctx.Has("Student2") && ctx.Has("Business2"))
        {
            var d = await api.PostAsync("/api/attendance", new
            {
                studentId = ctx.Get("Student2"),
                businessId = ctx.Get("Business2"),
                institutionId,
                date = now.AddDays(-3),
                absenceType = "Unexcused",
                markedBy = "Mustafa Yılmaz"
            });
            if (d is not null)
            {
                ctx.Set("Attendance4", d.Value.GetProperty("attendanceId").GetGuid());
                Console.WriteLine("  ✓ Devamsızlık 4: Mazeretsiz (doğrulanmamış)");
            }
        }

        // Attendance 5: Mazeretsiz → düzeltildi → mazeretli
        if (ctx.Has("Student1") && ctx.Has("Business1"))
        {
            var d = await api.PostAsync("/api/attendance", new
            {
                studentId = ctx.Get("Student1"),
                businessId = ctx.Get("Business1"),
                institutionId,
                date = now.AddDays(-20),
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

        // Attendance 6: Mazeretsiz — kaydedilmiş
        if (ctx.Has("Student1") && ctx.Has("Business1"))
        {
            var d = await api.PostAsync("/api/attendance", new
            {
                studentId = ctx.Get("Student1"),
                businessId = ctx.Get("Business1"),
                institutionId,
                date = now.AddDays(-5),
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
