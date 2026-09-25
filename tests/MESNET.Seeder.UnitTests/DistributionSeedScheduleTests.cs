using MESNET.Seeder.Seeders;
using Shouldly;
using Xunit;

namespace MESNET.Seeder.UnitTests;

/// <summary>
/// Seed edilen öğretmen ders programı dağıtım zincirini besleyebilmeli (#312).
///
/// <para><b>Yaşanan hata:</b> seeder hiç program üretmiyordu; boş slot türetmek ve slota işletme
/// atamak programa bağlı olduğu için dağıtım ekranı seed sonrası boş açılıyordu
/// (<c>free-slots</c> → 422 <c>Coordination.ScheduleNotFound</c>). Tamamen dolu bir program da
/// aynı sonucu verir: atanabilecek boş slot kalmaz.</para>
/// </summary>
public sealed class DistributionSeedScheduleTests
{
    private static readonly string[] Weekdays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"];

    [Fact]
    public void Program_hafta_ici_bes_gunu_ve_gunluk_ders_sayisini_kapsar()
    {
        var schedule = DistributionSeeder.WeeklySchedule(teacherIndex: 0, branchCode: "EET");

        schedule.Select(d => d.Day).ShouldBe(Weekdays);
        schedule.ShouldAllBe(d => d.Periods.Count == DistributionSeeder.DailyPeriodCount);
    }

    /// <summary><b>Asıl regresyon:</b> her gün hem dolu hem boş slot bulunur.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Her_gun_hem_dolu_hem_bos_slot_vardir(int teacherIndex)
    {
        var schedule = DistributionSeeder.WeeklySchedule(teacherIndex, "BT");

        schedule.ShouldAllBe(d =>
            d.Periods.Any(p => p.Status == "Free") && d.Periods.Any(p => p.Status == "Occupied"));
    }

    [Fact]
    public void Bos_slotta_ders_adi_yoktur()
    {
        DistributionSeeder.WeeklySchedule(0, "MTT")
            .SelectMany(d => d.Periods)
            .Where(p => p.Status == "Free")
            .ShouldAllBe(p => p.CourseName == null);
    }

    /// <summary>Öğretmenler aynı boş saatlere yığılmasın — dağıtım ekranında fark görünsün.</summary>
    [Fact]
    public void Farkli_ogretmenlerin_programi_farklidir()
    {
        var first = DistributionSeeder.WeeklySchedule(0, "EET").SelectMany(d => d.Periods).Select(p => p.Status);
        var second = DistributionSeeder.WeeklySchedule(1, "EET").SelectMany(d => d.Periods).Select(p => p.Status);

        first.ShouldNotBe(second);
    }
}
