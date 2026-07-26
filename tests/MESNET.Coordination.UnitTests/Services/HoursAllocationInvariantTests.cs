using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.Services;
using Shouldly;
using Xunit;

using static MESNET.Coordination.UnitTests.Services.AllocationTestData;

namespace MESNET.Coordination.UnitTests.Services;

/// <summary>
/// Issue #116 kabul kriterleri — havuzun her değeri için değişmez (invariant) doğrulaması.
/// </summary>
public class HoursAllocationInvariantTests
{
    private static AllocationInput[] Portfolio() =>
    [
        Business(1, maxHours: 8, studentCount: 5),
        Business(2, maxHours: 6, studentCount: 3),
        Business(3, maxHours: 4, studentCount: 2),
        Business(4, maxHours: 2, studentCount: 1),
        Business(5, maxHours: 8, studentCount: 0),
        Business(6, maxHours: 6, studentCount: 9)
    ];

    public static TheoryData<int> Pools()
    {
        var data = new TheoryData<int>();
        for (var pool = 0; pool <= 45; pool++) data.Add(pool);
        return data;
    }

    [Theory]
    [MemberData(nameof(Pools))]
    public void total_suggested_hours_never_exceed_the_pool(int pool)
    {
        // Act
        var result = HoursAllocationCalculator.Allocate(Portfolio(), pool, teacherCapacity: 12);

        // Assert
        result.Lines.Sum(l => l.SuggestedHours).ShouldBeLessThanOrEqualTo(pool);
        result.Diagnostics.TotalAllocated.ShouldBe(result.Lines.Sum(l => l.SuggestedHours));
    }

    [Theory]
    [MemberData(nameof(Pools))]
    public void every_paid_line_stays_between_one_and_its_max(int pool)
    {
        // Act
        var result = HoursAllocationCalculator.Allocate(Portfolio(), pool, teacherCapacity: 12);

        // Assert
        foreach (var line in result.Lines.Where(l => !l.IsHonoraryVisit))
        {
            line.SuggestedHours.ShouldBeGreaterThanOrEqualTo(1);
            line.SuggestedHours.ShouldBeLessThanOrEqualTo(line.MaxHours);
        }
    }

    [Theory]
    [MemberData(nameof(Pools))]
    public void honorary_lines_always_carry_zero_hours(int pool)
    {
        // Act
        var result = HoursAllocationCalculator.Allocate(Portfolio(), pool, teacherCapacity: 12);

        // Assert
        result.Lines.Where(l => l.IsHonoraryVisit).ShouldAllBe(l => l.SuggestedHours == 0);
        result.Diagnostics.HonoraryCount.ShouldBe(result.Lines.Count(l => l.IsHonoraryVisit));
    }

    [Theory]
    [MemberData(nameof(Pools))]
    public void undistributed_is_the_gap_between_pool_and_allocation(int pool)
    {
        // Act
        var result = HoursAllocationCalculator.Allocate(Portfolio(), pool, teacherCapacity: 12);

        // Assert
        var expected = result.Diagnostics.IsPoolUndefined
            ? 0
            : pool - result.Diagnostics.TotalAllocated;
        result.Diagnostics.Undistributed.ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(Pools))]
    public void pinned_rows_keep_their_hours_for_every_pool_size(int pool)
    {
        // Arrange
        AllocationInput[] inputs =
        [
            Pinned(1, maxHours: 8, studentCount: 5, pinnedHours: 6),
            Pinned(2, maxHours: 4, studentCount: 2, pinnedHours: 1),
            Business(3, maxHours: 6, studentCount: 3),
            Business(4, maxHours: 4, studentCount: 2)
        ];

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool, teacherCapacity: 12);

        // Assert
        if (result.Diagnostics.IsPoolUndefined)
        {
            result.Lines.ShouldBeEmpty();
            return;
        }

        result.HoursOf(1).ShouldBe(6);
        result.HoursOf(2).ShouldBe(1);
    }

    [Theory]
    [MemberData(nameof(Pools))]
    public void out_of_branch_hours_match_the_out_of_branch_bucket(int pool)
    {
        // Act
        var result = HoursAllocationCalculator.Allocate(Portfolio(), pool, teacherCapacity: 12);

        // Assert
        var expected = result.Lines
            .Where(l => l.Bucket == AllocationBucket.OutOfBranchSuggested)
            .Sum(l => l.SuggestedHours);
        result.Diagnostics.OutOfBranchHours.ShouldBe(expected);
    }
}
