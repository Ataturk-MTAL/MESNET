using MESNET.Coordination.Core.Services;
using Shouldly;
using Xunit;

using static MESNET.Coordination.UnitTests.Services.AllocationTestData;

namespace MESNET.Coordination.UnitTests.Services;

/// <summary>
/// Alan öğretmeni kapasitesi <c>C</c> (issue #116):
/// <c>Σ max(0, min(boş slot toplamı, MaxWeeklyExtraHours − mevcut atanmış))</c>.
/// </summary>
public class TeacherCapacityCalculatorTests
{
    private static TeacherCapacityInput Teacher(int id, int freeSlots, int assignedHours) =>
        new(Id(id), freeSlots, assignedHours);

    [Fact]
    public void free_slots_bind_when_they_are_below_the_remaining_extra_hours_quota()
    {
        // Arrange — kota 20, atanmış 4 → kalan kota 16; boş slot yalnız 5
        var teachers = new[] { Teacher(1, freeSlots: 5, assignedHours: 4) };

        // Act
        var capacity = TeacherCapacityCalculator.Calculate(teachers, maxWeeklyExtraHours: 20);

        // Assert — küçük olan tavan bağlar
        capacity.ShouldBe(5);
    }

    [Fact]
    public void remaining_quota_binds_when_it_is_below_the_free_slot_count()
    {
        // Arrange — kota 20, atanmış 18 → kalan kota 2; boş slot 9
        var teachers = new[] { Teacher(1, freeSlots: 9, assignedHours: 18) };

        // Act
        var capacity = TeacherCapacityCalculator.Calculate(teachers, maxWeeklyExtraHours: 20);

        // Assert
        capacity.ShouldBe(2);
    }

    [Fact]
    public void capacities_of_all_branch_teachers_are_summed()
    {
        // Arrange
        var teachers = new[]
        {
            Teacher(1, freeSlots: 5, assignedHours: 4),   // min(5, 16) = 5
            Teacher(2, freeSlots: 9, assignedHours: 18),  // min(9, 2)  = 2
            Teacher(3, freeSlots: 12, assignedHours: 0)   // min(12, 20) = 12
        };

        // Act
        var capacity = TeacherCapacityCalculator.Calculate(teachers, maxWeeklyExtraHours: 20);

        // Assert
        capacity.ShouldBe(19);
    }

    [Fact]
    public void a_teacher_over_the_quota_contributes_zero_not_a_negative_number()
    {
        // Arrange — atanmış 26, kota 20 → kalan −6; toplamı aşağı çekmemeli
        var teachers = new[]
        {
            Teacher(1, freeSlots: 4, assignedHours: 26),
            Teacher(2, freeSlots: 4, assignedHours: 0)
        };

        // Act
        var capacity = TeacherCapacityCalculator.Calculate(teachers, maxWeeklyExtraHours: 20);

        // Assert — 0 + 4
        capacity.ShouldBe(4);
    }

    [Fact]
    public void a_teacher_without_free_slots_contributes_nothing()
    {
        var teachers = new[] { Teacher(1, freeSlots: 0, assignedHours: 0) };

        TeacherCapacityCalculator.Calculate(teachers, maxWeeklyExtraHours: 20).ShouldBe(0);
    }

    [Fact]
    public void missing_configuration_yields_zero_capacity_instead_of_unlimited()
    {
        // Arrange — CoordinationConfig okunamadığında 0 gelir; "sınırsız" varsayılmaz
        var teachers = new[] { Teacher(1, freeSlots: 30, assignedHours: 0) };

        // Act
        var capacity = TeacherCapacityCalculator.Calculate(teachers, maxWeeklyExtraHours: 0);

        // Assert
        capacity.ShouldBe(0);
    }

    [Fact]
    public void no_teacher_means_no_capacity()
    {
        TeacherCapacityCalculator.Calculate([], maxWeeklyExtraHours: 20).ShouldBe(0);
    }

    [Fact]
    public void negative_inputs_are_normalized_to_zero()
    {
        // Arrange — bozuk projeksiyon negatif değer üretse bile kapasite şişmez
        var teachers = new[] { Teacher(1, freeSlots: -3, assignedHours: -5) };

        // Act
        var capacity = TeacherCapacityCalculator.Calculate(teachers, maxWeeklyExtraHours: 20);

        // Assert — boş slot 0'a kırpılır → min(0, 20) = 0
        capacity.ShouldBe(0);
    }

    [Fact]
    public void null_teacher_list_is_rejected()
    {
        Should.Throw<ArgumentNullException>(
            () => TeacherCapacityCalculator.Calculate(null!, maxWeeklyExtraHours: 20));
    }

    [Fact]
    public void single_teacher_capacity_is_exposed_for_per_teacher_diagnostics()
    {
        TeacherCapacityCalculator
            .CapacityOf(Teacher(1, freeSlots: 7, assignedHours: 15), maxWeeklyExtraHours: 20)
            .ShouldBe(5);
    }

    [Fact]
    public void capacity_feeds_the_allocation_bucket_split()
    {
        // Arrange — kapasite 3 saat; havuz 6 saatlik iki işletmeye yetiyor
        var capacity = TeacherCapacityCalculator.Calculate(
            [Teacher(1, freeSlots: 3, assignedHours: 0)], maxWeeklyExtraHours: 20);

        var inputs = new[]
        {
            Business(1, maxHours: 4, studentCount: 5),  // w = 20
            Business(2, maxHours: 4, studentCount: 1)   // w = 4
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 6, teacherCapacity: capacity);

        // Assert — kapasite tükendikten sonraki satırlar alan dışına önerilir
        capacity.ShouldBe(3);
        result.Diagnostics.TeacherCapacity.ShouldBe(3);
        result.Diagnostics.OutOfBranchHours.ShouldBeGreaterThan(0);
    }
}
