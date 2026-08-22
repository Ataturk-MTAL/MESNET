using MESNET.Coordination.Core.Services;
using Shouldly;
using Xunit;

using static MESNET.Coordination.UnitTests.Services.AllocationTestData;

namespace MESNET.Coordination.UnitTests.Services;

/// <summary>
/// Kilitli satırların sorgu dizesi gösterimi (#116/#118). Bozuk girdi sessizce
/// atlanmaz — atlanırsa koordinatörün elle girdiği saat yok olurdu.
/// </summary>
public class PinnedHoursSelectionTests
{
    [Fact]
    public void empty_input_means_no_pinned_row_and_no_error()
    {
        PinnedHoursSelection.TryParse(null, out var pinned, out var error).ShouldBeTrue();
        pinned.ShouldBeEmpty();
        error.ShouldBeNull();

        PinnedHoursSelection.TryParse("   ", out pinned, out error).ShouldBeTrue();
        pinned.ShouldBeEmpty();
        error.ShouldBeNull();
    }

    [Fact]
    public void pairs_are_parsed_in_order()
    {
        var raw = $"{Id(1)}:6,{Id(2)}:0";

        PinnedHoursSelection.TryParse(raw, out var pinned, out var error).ShouldBeTrue();

        error.ShouldBeNull();
        pinned.Count.ShouldBe(2);
        pinned[0].ShouldBe(new PinnedHours(Id(1), 6));
        // 0 saat = fahri olarak kilitlendi
        pinned[1].ShouldBe(new PinnedHours(Id(2), 0));
    }

    [Fact]
    public void surrounding_whitespace_is_tolerated()
    {
        var raw = $" {Id(1)}:6 , {Id(2)}:3 ";

        PinnedHoursSelection.TryParse(raw, out var pinned, out _).ShouldBeTrue();

        pinned.Count.ShouldBe(2);
    }

    [Fact]
    public void a_malformed_pair_fails_the_whole_selection()
    {
        var raw = $"{Id(1)}:6,bozuk";

        PinnedHoursSelection.TryParse(raw, out var pinned, out var error).ShouldBeFalse();

        pinned.ShouldBeEmpty();
        error.ShouldNotBeNull();
        error.ShouldContain("bozuk");
    }

    [Fact]
    public void an_invalid_business_id_is_reported()
    {
        PinnedHoursSelection.TryParse("abc:6", out _, out var error).ShouldBeFalse();

        error.ShouldNotBeNull();
        error.ShouldContain("işletme kimliği");
    }

    [Fact]
    public void an_empty_guid_is_rejected()
    {
        PinnedHoursSelection.TryParse($"{Guid.Empty}:6", out _, out var error).ShouldBeFalse();

        error.ShouldNotBeNull();
    }

    [Fact]
    public void a_negative_hour_is_rejected()
    {
        PinnedHoursSelection.TryParse($"{Id(1)}:-2", out _, out var error).ShouldBeFalse();

        error.ShouldNotBeNull();
        error.ShouldContain("saat");
    }

    [Fact]
    public void a_non_numeric_hour_is_rejected()
    {
        PinnedHoursSelection.TryParse($"{Id(1)}:altı", out _, out var error).ShouldBeFalse();

        error.ShouldNotBeNull();
    }

    [Fact]
    public void the_same_business_cannot_be_pinned_twice()
    {
        var raw = $"{Id(1)}:6,{Id(1)}:4";

        PinnedHoursSelection.TryParse(raw, out _, out var error).ShouldBeFalse();

        error.ShouldNotBeNull();
        error.ShouldContain("birden çok kez");
    }

    [Fact]
    public void parsed_pins_feed_the_allocator_unchanged()
    {
        // Arrange — 1 numaralı işletme 6 saatte kilitli
        PinnedHoursSelection.TryParse($"{Id(1)}:6", out var pinned, out _).ShouldBeTrue();

        var inputs = new[]
        {
            new AllocationInput(Id(1), DefaultBranch, MaxHours: 8, StudentCount: 1,
                IsPinned: true, PinnedHours: pinned[0].Hours),
            Business(2, maxHours: 8, studentCount: 5)
        };

        // Act
        var result = HoursAllocationCalculator.Allocate(inputs, pool: 14, teacherCapacity: UnlimitedCapacity);

        // Assert — kilitli satır değişmez
        result.HoursOf(1).ShouldBe(6);
        result.LineOf(1).IsPinned.ShouldBeTrue();
        result.HoursOf(2).ShouldBe(8);
    }
}
