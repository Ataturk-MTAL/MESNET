using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.Services;
using Shouldly;
using Xunit;

using static MESNET.Coordination.UnitTests.Services.AllocationTestData;

namespace MESNET.Coordination.UnitTests.Services;

/// <summary>
/// Üç kova (alan içi ücretli / alan dışı öneri / fahri) ve öğretmen kapasitesi kesimi.
/// </summary>
public class HoursAllocationBucketTests
{
    [Fact]
    public void everything_stays_in_branch_when_teacher_capacity_covers_the_pool()
    {
        // Arrange
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),
            Business(2, maxHours: 6, studentCount: 3),
            Business(3, maxHours: 4, studentCount: 2)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: 10);

        // Assert
        result.Lines.ShouldAllBe(l => l.Bucket == AllocationBucket.InBranchPaid);
        result.Diagnostics.OutOfBranchHours.ShouldBe(0);
    }

    [Fact]
    public void businesses_below_the_capacity_cut_are_suggested_out_of_branch()
    {
        // Arrange — saatler 5 / 3 / 2, kapasite 6
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),  // w = 40 → 5 saat
            Business(2, maxHours: 6, studentCount: 3),  // w = 18 → 3 saat
            Business(3, maxHours: 4, studentCount: 2)   // w =  8 → 2 saat
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: 6);

        // Assert — kümülatif 5 ≤ 6, 8 > 6 → 2 ve sonrası alan dışına önerilir
        result.LineOf(1).Bucket.ShouldBe(AllocationBucket.InBranchPaid);
        result.LineOf(2).Bucket.ShouldBe(AllocationBucket.OutOfBranchSuggested);
        result.LineOf(3).Bucket.ShouldBe(AllocationBucket.OutOfBranchSuggested);
        result.Diagnostics.OutOfBranchHours.ShouldBe(5);
    }

    [Fact]
    public void zero_teacher_capacity_pushes_every_paid_line_out_of_branch()
    {
        // Arrange
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),
            Business(2, maxHours: 6, studentCount: 3)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: 0);

        // Assert
        result.Lines.ShouldAllBe(l => l.Bucket == AllocationBucket.OutOfBranchSuggested);
        result.Diagnostics.OutOfBranchHours.ShouldBe(10);
    }

    [Fact]
    public void out_of_branch_and_honorary_buckets_can_appear_at_the_same_time()
    {
        // Arrange — havuz 3 (Σ max = 20'nin altında), kapasite 1 (havuzun altında)
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),  // w = 40
            Business(2, maxHours: 6, studentCount: 3),  // w = 18
            Business(3, maxHours: 4, studentCount: 2),  // w =  8
            Business(4, maxHours: 2, studentCount: 1)   // w =  2
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 3, teacherCapacity: 1);

        // Assert
        result.LineOf(1).Bucket.ShouldBe(AllocationBucket.InBranchPaid);
        result.LineOf(2).Bucket.ShouldBe(AllocationBucket.OutOfBranchSuggested);
        result.LineOf(3).Bucket.ShouldBe(AllocationBucket.OutOfBranchSuggested);
        result.LineOf(4).Bucket.ShouldBe(AllocationBucket.Honorary);
        result.Diagnostics.OutOfBranchHours.ShouldBe(2);
        result.Diagnostics.HonoraryCount.ShouldBe(1);
    }

    [Fact]
    public void pinned_lines_take_part_in_the_capacity_ordering()
    {
        // Arrange — sabitlenmiş 7 saat en yüksek ağırlıkta, kapasite 7
        var inputs = new[]
        {
            Pinned(1, maxHours: 8, studentCount: 5, pinnedHours: 7),
            Business(2, maxHours: 6, studentCount: 3),
            Business(3, maxHours: 4, studentCount: 2)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: 7);

        // Assert
        result.LineOf(1).Bucket.ShouldBe(AllocationBucket.InBranchPaid);
        result.LineOf(2).Bucket.ShouldBe(AllocationBucket.OutOfBranchSuggested);
        result.LineOf(3).Bucket.ShouldBe(AllocationBucket.OutOfBranchSuggested);
        result.Diagnostics.OutOfBranchHours.ShouldBe(3);
    }

    [Fact]
    public void bucket_slugs_are_turkish_for_the_ui()
    {
        AllocationBucket.InBranchPaid.Slug.ShouldBe("Alan içi ücretli");
        AllocationBucket.OutOfBranchSuggested.Slug.ShouldBe("Alan dışı öneri");
        AllocationBucket.Honorary.Slug.ShouldBe("Fahri");
    }
}
