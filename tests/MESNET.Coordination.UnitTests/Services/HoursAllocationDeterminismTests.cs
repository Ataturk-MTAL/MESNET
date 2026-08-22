using MESNET.Coordination.Core.Services;
using Shouldly;
using Xunit;

using static MESNET.Coordination.UnitTests.Services.AllocationTestData;

namespace MESNET.Coordination.UnitTests.Services;

/// <summary>
/// Determinizm ve beraberlik bozma (tie-break) kuralı:
/// ağırlık ↓ → tavan saat ↓ → alan kodu (ordinal) ↑ → işletme kimliği (ordinal) ↑
/// </summary>
public class HoursAllocationDeterminismTests
{
    [Fact]
    public void same_input_produces_the_same_output_every_time()
    {
        // Arrange
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),
            Business(2, maxHours: 6, studentCount: 3),
            Business(3, maxHours: 4, studentCount: 2),
            Pinned(4, maxHours: 8, studentCount: 7, pinnedHours: 3)
        };

        // Act
        var first = HoursAllocationCalculator.Allocate(inputs, pool: 13, teacherCapacity: 7);
        var second = HoursAllocationCalculator.Allocate(inputs, pool: 13, teacherCapacity: 7);

        // Assert
        second.Lines.ShouldBe(first.Lines);
        second.Diagnostics.ShouldBe(first.Diagnostics);
    }

    [Fact]
    public void input_ordering_does_not_change_the_result()
    {
        // Arrange
        var ascending = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),
            Business(2, maxHours: 6, studentCount: 3),
            Business(3, maxHours: 4, studentCount: 2)
        };
        var descending = ascending.Reverse().ToArray();

        // Act
        var first = HoursAllocationCalculator.Allocate(ascending, pool: 10, teacherCapacity: 6);
        var second = HoursAllocationCalculator.Allocate(descending, pool: 10, teacherCapacity: 6);

        // Assert
        second.Lines.ShouldBe(first.Lines);
    }

    [Fact]
    public void lines_are_returned_in_priority_order()
    {
        // Arrange
        var inputs = new[]
        {
            Business(3, maxHours: 4, studentCount: 2),  // w =  8
            Business(1, maxHours: 8, studentCount: 5),  // w = 40
            Business(2, maxHours: 6, studentCount: 3)   // w = 18
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: 6);

        // Assert
        result.Lines.Select(l => l.BusinessId).ShouldBe([Id(1), Id(2), Id(3)]);
    }

    [Fact]
    public void equal_weights_are_broken_by_the_higher_max_hours()
    {
        // Arrange — 1 ve 2 numaralı işletmelerin ağırlığı da 8; havuz yalnız birine yetiyor
        var inputs = new[]
        {
            Business(1, maxHours: 4, studentCount: 2),
            Business(2, maxHours: 8, studentCount: 1),
            Business(3, maxHours: 2, studentCount: 1)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 1, teacherCapacity: UnlimitedCapacity);

        // Assert — tavanı yüksek olan (uzak işletme) önce fonlanır
        result.HoursOf(2).ShouldBe(1);
        result.LineOf(1).IsHonoraryVisit.ShouldBeTrue();
        result.LineOf(3).IsHonoraryVisit.ShouldBeTrue();
    }

    [Fact]
    public void equal_weight_and_max_hours_are_broken_by_the_branch_code()
    {
        // Arrange — aynı ağırlık ve tavan; alan kodu ordinal olarak küçük olan önce gelir
        var inputs = new[]
        {
            Business(1, maxHours: 4, studentCount: 2, branchCode: "ZZZ"),
            Business(2, maxHours: 4, studentCount: 2, branchCode: "AAA")
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 1, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(2).ShouldBe(1);
        result.LineOf(1).IsHonoraryVisit.ShouldBeTrue();
    }

    [Fact]
    public void fully_identical_rows_are_broken_by_the_business_id()
    {
        // Arrange — her şeyi aynı iki satır; yalnız kimlik farklı
        var inputs = new[]
        {
            Business(2, maxHours: 4, studentCount: 2),
            Business(1, maxHours: 4, studentCount: 2)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 1, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(1).ShouldBe(1);
        result.LineOf(2).IsHonoraryVisit.ShouldBeTrue();
    }
}
