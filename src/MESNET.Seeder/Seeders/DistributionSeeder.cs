namespace MESNET.Seeder.Seeders;

/// <summary>
/// İş yeri dağıtımının iki ön koşulu (#312): öğretmen ders programı ve alan ders yükü
/// yapılandırması.
///
/// <para><b>Neden ayrı adım:</b> ikisi de eksikken dağıtım ekranı seed sonrası boş açılıyordu —
/// program yoksa boş slot türetilemez (<c>free-slots</c> → 422 <c>ScheduleNotFound</c>), yük
/// yapılandırması yoksa saat havuzu sıfırdır (<c>isPoolUndefined: true</c>). API nedeni
/// söylüyordu; eksik olan demo verisiydi.</para>
/// </summary>
public static class DistributionSeeder
{
    /// <summary>Kurumun günlük ders sayısı — <see cref="InstitutionSeeder"/> aynı değeri yazar.</summary>
    public const int DailyPeriodCount = 8;

    private const string Semester = "Fall";
    private const int AcademicYear = 2025;

    private static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];

    public sealed record PeriodSlot(int PeriodNumber, string Status, string? CourseName);

    public sealed record DailySchedule(string Day, IReadOnlyList<PeriodSlot> Periods);

    public static async Task SeedAsync(MesnetApiClient api, SeedContext ctx)
    {
        Console.WriteLine();
        Console.WriteLine("── Dağıtım ön koşulları ───────────");

        if (!ctx.Has("AcademicPeriod"))
        {
            Console.WriteLine("  ⚠ Akademik dönem yok — ders programı ve ders yükü atlanıyor");
            return;
        }
        var academicPeriodId = ctx.Get("AcademicPeriod");

        await SyncStudentCountsAsync(api, ctx.Get("Institution"), academicPeriodId);
        await SeedBranchWorkloadsAsync(api, academicPeriodId);
        await SeedTeacherSchedulesAsync(api, ctx, academicPeriodId);
    }

    /// <summary>
    /// Haftalık program: her gün ilk 4–6 ders dolu, kalanı boş. Dolu ders sayısı öğretmene
    /// göre kayar ki boş slotlar aynı saatlere yığılmasın. Saf fonksiyon — G/Ç yapmaz.
    /// </summary>
    public static IReadOnlyList<DailySchedule> WeeklySchedule(int teacherIndex, string branchCode) =>
        Weekdays.Select((day, dayIndex) =>
        {
            var occupiedCount = 4 + (dayIndex + teacherIndex) % 3;
            var periods = Enumerable.Range(1, DailyPeriodCount)
                .Select(period => period <= occupiedCount
                    ? new PeriodSlot(period, "Occupied", $"{branchCode} Meslek Dersi")
                    : new PeriodSlot(period, "Free", null))
                .ToList();
            return new DailySchedule(day, periods);
        }).ToList();

    /// <summary>
    /// Ders yükü grup sayısını <b>yazma anında</b> Coordination'ın öğrenci sayacından hesaplar
    /// ve dondurur. Sayaç <c>StudentRegistered</c> olaylarıyla asenkron dolar; seeder öğrencileri
    /// az önce kaydettiği için sayaç henüz eksiktir. Ölçüldü: her alanda 40 öğrenci varken sayaç
    /// EET 1 gösteriyordu, grup sayısı 0 çıktı ve havuz yalnız şef saatlerinden (16) oluştu.
    /// Mutlak eşitleme Enrollment'taki gerçek sayıları yayınlar; tüketicinin işlemesi beklenir.
    /// </summary>
    private static async Task SyncStudentCountsAsync(MesnetApiClient api, Guid institutionId, Guid academicPeriodId)
    {
        var result = await api.PostAsync("/api/students/sync-counts", new { institutionId, academicPeriodId });
        if (result is null)
        {
            Console.WriteLine("  ⚠ Öğrenci sayısı eşitlenemedi — ders yükü havuzu eksik hesaplanabilir");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(3));
        Console.WriteLine("  ✓ Alan öğrenci sayıları eşitlendi");
    }

    /// <summary>
    /// Alan başına ders yükü. Değerler #312 incelemesinde elle girilip zinciri uçtan uca
    /// çalıştıran yapılandırmadır. Uç <c>PUT</c>'tur — her koşuda güvenle tekrarlanır.
    /// </summary>
    private static async Task SeedBranchWorkloadsAsync(MesnetApiClient api, Guid academicPeriodId)
    {
        var branchCodes = EnrollmentSeeder.Teachers.Select(t => t.BranchCode).Distinct();

        foreach (var branchCode in branchCodes)
        {
            var result = await api.PutAsync($"/api/coordination/teachers/branch-workload/{branchCode}", new
            {
                // Kurum ve alan uçta istekten değil token'dan/rotadan konur; gövde bağlanabilsin diye var.
                institutionId = Guid.Empty,
                academicPeriodId,
                branchCode,
                educationType = "Formal",
                departmentHeadCount = 1,
                workshopHeadCount = 1,
                departmentHeadHours = 10,
                workshopHeadHours = 6,
                classLevels = new[]
                {
                    new { classYear = 11, weeklyLessonHours = 24 },
                    new { classYear = 12, weeklyLessonHours = 24 },
                },
            });

            Console.WriteLine(result is null
                ? $"  ✗ {branchCode} ders yükü yapılandırılamadı"
                : $"  ✓ {branchCode} ders yükü yapılandırıldı");
        }
    }

    /// <summary>
    /// Öğretmen ders programları. Program olay kaynaklıdır: her POST geçmişe yeni bir sürüm
    /// ekler. Bu yüzden mevcut program varsa dokunulmaz — tekrar koşu geçmişi şişirmez.
    /// </summary>
    private static async Task SeedTeacherSchedulesAsync(MesnetApiClient api, SeedContext ctx, Guid academicPeriodId)
    {
        foreach (var (teacher, index) in EnrollmentSeeder.Teachers.Select((t, i) => (t, i)))
        {
            if (!ctx.Has(teacher.Key)) continue;
            var teacherId = ctx.Get(teacher.Key);

            var current = await api.GetAsync(
                $"/api/coordination/teachers/{teacherId}/schedule/current?academicPeriodId={academicPeriodId}&semester={Semester}");
            if (current is not null)
            {
                Console.WriteLine($"  → \"{teacher.Name}\" ders programı mevcut");
                continue;
            }

            var result = await api.PostAsync($"/api/coordination/teachers/{teacherId}/schedule", new
            {
                teacherId,
                // Uç kurumu hâlâ gövdeden alıyor (#235 borcu) — kiracıyla aynı okul gönderilmeli;
                // Program.cs bunu okul adımlarından önce doğrular (#309).
                institutionId = ctx.Get("Institution"),
                academicPeriodId,
                academicYear = AcademicYear,
                semester = Semester,
                weeklySchedule = WeeklySchedule(index, teacher.BranchCode),
            });

            Console.WriteLine(result is null
                ? $"  ✗ \"{teacher.Name}\" ders programı oluşturulamadı"
                : $"  ✓ \"{teacher.Name}\" ({teacher.BranchCode}) ders programı oluşturuldu");
        }
    }
}
