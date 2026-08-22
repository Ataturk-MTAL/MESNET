using MESNET.Coordination.Core.Enums;
using MESNET.Coordination.Core.Services;
using Shouldly;
using Xunit;

using static MESNET.Coordination.UnitTests.Services.AllocationTestData;

namespace MESNET.Coordination.UnitTests.Services;

/// <summary>
/// Issue #116'da sayılan sınır durumlarının her biri için ayrı test.
/// </summary>
public class HoursAllocationEdgeCaseTests
{
    [Fact]
    public void pool_larger_than_sum_of_max_saturates_everyone_and_reports_the_surplus()
    {
        // Arrange — Σ max = 18, havuz 20
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),
            Business(2, maxHours: 6, studentCount: 3),
            Business(3, maxHours: 4, studentCount: 2)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 20, teacherCapacity: UnlimitedCapacity);

        // Assert — herkes tavanında, artan havuz sessizce yutulmaz
        result.Lines.ShouldAllBe(l => l.SuggestedHours == l.MaxHours);
        result.Diagnostics.SumOfMax.ShouldBe(18);
        result.Diagnostics.TotalAllocated.ShouldBe(18);
        result.Diagnostics.Undistributed.ShouldBe(2);
    }

    [Fact]
    public void pool_smaller_than_business_count_moves_the_lowest_weights_to_honorary()
    {
        // Arrange — 4 işletme, 2 saatlik havuz
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),  // w = 40
            Business(2, maxHours: 6, studentCount: 3),  // w = 18
            Business(3, maxHours: 4, studentCount: 2),  // w = 8
            Business(4, maxHours: 2, studentCount: 1)   // w = 2
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 2, teacherCapacity: UnlimitedCapacity);

        // Assert — en düşük ağırlıklı ikisi fahri, sessiz kırpma yok
        result.HoursOf(1).ShouldBe(1);
        result.HoursOf(2).ShouldBe(1);
        result.LineOf(3).IsHonoraryVisit.ShouldBeTrue();
        result.LineOf(3).SuggestedHours.ShouldBe(0);
        result.LineOf(4).IsHonoraryVisit.ShouldBeTrue();
        result.LineOf(4).SuggestedHours.ShouldBe(0);
        result.Diagnostics.HonoraryCount.ShouldBe(2);
        result.Diagnostics.TotalAllocated.ShouldBe(2);
    }

    [Fact]
    public void honorary_lines_are_marked_with_the_honorary_bucket()
    {
        // Arrange
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),
            Business(2, maxHours: 2, studentCount: 1)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 1, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.LineOf(2).Bucket.ShouldBe(AllocationBucket.Honorary);
        result.LineOf(2).Bucket.Slug.ShouldBe("Fahri");
    }

    [Fact]
    public void zero_pool_produces_no_suggestion_and_flags_the_pool_as_undefined()
    {
        // Arrange
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 5),
            Business(2, maxHours: 6, studentCount: 3)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 0, teacherCapacity: UnlimitedCapacity);

        // Assert — "havuz tanımlanmamış" ayrımı çıktıda görünür
        result.Diagnostics.IsPoolUndefined.ShouldBeTrue();
        result.Lines.ShouldBeEmpty();
        result.Diagnostics.TotalAllocated.ShouldBe(0);
        result.Diagnostics.HonoraryCount.ShouldBe(0);
        result.Diagnostics.Undistributed.ShouldBe(0);
        result.Diagnostics.SumOfMax.ShouldBe(14);
    }

    [Fact]
    public void negative_pool_is_treated_as_undefined_pool()
    {
        // Arrange
        var inputs = new[] { Business(1, maxHours: 8, studentCount: 5) };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: -5, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.Diagnostics.IsPoolUndefined.ShouldBeTrue();
        result.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void business_without_students_gets_only_the_floor_hour_even_when_the_pool_is_large()
    {
        // Arrange — 1 numaralı işletmede alanın öğrencisi yok → w = 0
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 0),
            Business(2, maxHours: 8, studentCount: 4)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 20, teacherCapacity: UnlimitedCapacity);

        // Assert — ağırlığı 0 olan işletme oransal paydan hiç almaz, yalnız taban 1 saatte kalır
        result.LineOf(1).Weight.ShouldBe(0);
        result.HoursOf(1).ShouldBe(1);
        result.HoursOf(2).ShouldBe(8);
        result.Diagnostics.TotalAllocated.ShouldBe(9);
        result.Diagnostics.Undistributed.ShouldBe(11);
    }

    [Fact]
    public void business_without_students_is_the_first_to_become_honorary_when_the_pool_is_tight()
    {
        // Arrange
        var inputs = new[]
        {
            Business(1, maxHours: 8, studentCount: 0),  // w = 0
            Business(2, maxHours: 8, studentCount: 4),  // w = 32
            Business(3, maxHours: 4, studentCount: 1)   // w = 4
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 2, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.LineOf(1).IsHonoraryVisit.ShouldBeTrue();
        result.LineOf(2).IsHonoraryVisit.ShouldBeFalse();
        result.LineOf(3).IsHonoraryVisit.ShouldBeFalse();
    }

    [Fact]
    public void all_businesses_pinned_leaves_every_hour_untouched()
    {
        // Arrange
        var inputs = new[]
        {
            Pinned(1, maxHours: 8, studentCount: 5, pinnedHours: 4),
            Pinned(2, maxHours: 6, studentCount: 3, pinnedHours: 3)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(1).ShouldBe(4);
        result.HoursOf(2).ShouldBe(3);
        result.Diagnostics.TotalAllocated.ShouldBe(7);
        result.Diagnostics.Undistributed.ShouldBe(3);
        result.Diagnostics.HonoraryCount.ShouldBe(0);
    }

    [Fact]
    public void pinned_hours_exceeding_the_pool_surface_as_negative_undistributed()
    {
        // Arrange — sabitlenmiş 8 saat, havuz 5 → koordinatörün elle aşımı gizlenmez
        var inputs = new[]
        {
            Pinned(1, maxHours: 8, studentCount: 5, pinnedHours: 8),
            Business(2, maxHours: 4, studentCount: 2)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 5, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(1).ShouldBe(8);
        result.LineOf(2).IsHonoraryVisit.ShouldBeTrue();
        result.Diagnostics.TotalAllocated.ShouldBe(8);
        result.Diagnostics.Undistributed.ShouldBe(-3);
    }

    [Fact]
    public void single_business_saturates_at_its_max_and_reports_the_rest_as_undistributed()
    {
        // Arrange
        var inputs = new[] { Business(1, maxHours: 8, studentCount: 5) };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(1).ShouldBe(8);
        result.Diagnostics.Undistributed.ShouldBe(2);
    }

    [Fact]
    public void single_business_receives_the_whole_pool_when_the_pool_is_below_its_max()
    {
        // Arrange
        var inputs = new[] { Business(1, maxHours: 8, studentCount: 5) };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 5, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(1).ShouldBe(5);
        result.Diagnostics.Undistributed.ShouldBe(0);
    }

    [Fact]
    public void empty_business_list_returns_no_lines_and_the_whole_pool_as_undistributed()
    {
        // Act
        var result = HoursAllocationCalculator.Allocate([], pool: 10, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.Lines.ShouldBeEmpty();
        result.Diagnostics.IsPoolUndefined.ShouldBeFalse();
        result.Diagnostics.SumOfMax.ShouldBe(0);
        result.Diagnostics.TotalAllocated.ShouldBe(0);
        result.Diagnostics.Undistributed.ShouldBe(10);
    }

    [Fact]
    public void business_with_zero_max_hours_can_only_be_an_honorary_visit()
    {
        // Arrange
        var inputs = new[]
        {
            Business(1, maxHours: 0, studentCount: 5),
            Business(2, maxHours: 4, studentCount: 2)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.HoursOf(1).ShouldBe(0);
        result.LineOf(1).IsHonoraryVisit.ShouldBeTrue();
        result.HoursOf(2).ShouldBe(4);
    }

    [Fact]
    public void negative_student_count_is_clamped_to_zero_weight()
    {
        // Arrange
        var inputs = new[] { Business(1, maxHours: 4, studentCount: -3) };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 10, teacherCapacity: UnlimitedCapacity);

        // Assert
        result.LineOf(1).Weight.ShouldBe(0);
        result.HoursOf(1).ShouldBe(1);
    }

    [Fact]
    public void null_business_list_is_rejected()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            HoursAllocationCalculator.Allocate(null!, pool: 10, teacherCapacity: 10));
    }
}
