using MESNET.Coordination.Core.Services;
using Shouldly;
using Xunit;

using static MESNET.Coordination.UnitTests.Services.AllocationTestData;

namespace MESNET.Coordination.UnitTests.Services;

/// <summary>
/// Çekirdek dağıtım davranışı: ağırlıklı oran, su-doldurma, en büyük kalan, sabitleme.
/// </summary>
public class HoursAllocationDistributionTests
{
    [Fact]
    public void weight_is_max_hours_times_student_count()
    {
        // Arrange
        var inputs = new[] { Business(1, maxHours: 8, studentCount: 5) };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.LineOf(1).Weight.ShouldBe(40);
    }

    [Fact]
    public void distributes_remaining_pool_proportionally_to_weight_with_largest_remainder()
    {
        // Arrange — ağırlıklar 40 / 18 / 8, taban 1 saat sonrası kalan havuz 7
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),
            Business(2, maxHours: 6, studentCount: 3),
            Business(3, maxHours: 4, studentCount: 2)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: UnlimitedCapacity);

        // Assert — 1+4=5, 1+1+1=3, 1+0+1=2 (kalanlar: 0.909 ve 0.848 en büyük iki kalan)
        result.HoursOf(1).ShouldBe(5);
        result.HoursOf(2).ShouldBe(3);
        result.HoursOf(3).ShouldBe(2);
        result.Diagnostics.TotalAllocated.ShouldBe(10);
        result.Diagnostics.Undistributed.ShouldBe(0);
    }

    [Fact]
    public void clips_to_max_hours_and_refills_surplus_to_unsaturated_businesses()
    {
        // Arrange — 1 numaralı işletme ezici ağırlığa sahip ama tavanı 2 saat
        var inputs = new[]
        {
            Business(1, maxHours: 2, studentCount: 50),  // w = 100, tavan 2
            Business(2, maxHours: 8, studentCount: 1),   // w = 8
            Business(3, maxHours: 8, studentCount: 1)    // w = 8
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 12, teacherCapacity: UnlimitedCapacity);

        // Assert — 1 tavanında kalır, kırpılan artık 2 ve 3'e akar
        result.HoursOf(1).ShouldBe(2);
        result.HoursOf(2).ShouldBe(5);
        result.HoursOf(3).ShouldBe(5);
        result.Diagnostics.TotalAllocated.ShouldBe(12);
        result.Diagnostics.Undistributed.ShouldBe(0);
    }

    [Fact]
    public void every_funded_business_gets_at_least_one_hour()
    {
        // Arrange — 3 numaralı işletmenin oransal payı 1 saatin altında
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 20),
            Business(2, maxHours: 8, studentCount: 20),
            Business(3, maxHours: 2, studentCount: 1)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 12, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(3).ShouldBeGreaterThanOrEqualTo(1);
        result.Lines.Where(l => !l.IsHonoraryVisit)
            .ShouldAllBe(l => l.SuggestedHours >= 1 && l.SuggestedHours <= l.MaxHours);
    }

    [Fact]
    public void pinned_hours_are_never_changed_and_are_deducted_from_the_pool()
    {
        // Arrange — 1 numaralı işletme 7 saate sabitlenmiş, geriye 3 saat kalıyor
        var inputs = new[]
        {
            Pinned(1, maxHours: 8, studentCount: 5, pinnedHours: 7),
            Business(2, maxHours: 6, studentCount: 3),
            Business(3, maxHours: 4, studentCount: 2)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(1).ShouldBe(7);
        result.LineOf(1).IsPinned.ShouldBeTrue();
        result.HoursOf(2).ShouldBe(2);
        result.HoursOf(3).ShouldBe(1);
        result.Diagnostics.TotalAllocated.ShouldBe(10);
    }

    [Fact]
    public void pinned_hours_above_their_own_max_are_preserved_verbatim()
    {
        // Arrange — koordinatörün elle girdiği değer tavanın üstünde olsa bile korunur
        var inputs = new[] { Pinned(1, maxHours: 4, studentCount: 2, pinnedHours: 6) };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(1).ShouldBe(6);
    }

    [Fact]
    public void reports_sum_of_max_and_pool_in_diagnostics()
    {
        // Arrange
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),
            Business(2, maxHours: 6, studentCount: 3)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 9, teacherCapacity: 4);

        // Assert
        result.Diagnostics.Pool.ShouldBe(9);
        result.Diagnostics.TeacherCapacity.ShouldBe(4);
        result.Diagnostics.SumOfMax.ShouldBe(14);
        result.Diagnostics.IsPoolUndefined.ShouldBeFalse();
    }
}
