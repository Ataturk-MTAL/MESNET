using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

/// <summary>
/// Ders saati durumu
/// </summary>
public sealed class SlotStatus : SmartEnum<SlotStatus>
{
    public static readonly SlotStatus Occupied = new(nameof(Occupied), 1, "Dolu");
    public static readonly SlotStatus Free = new(nameof(Free), 2, "Boş");

    private SlotStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public string Slug { get; }
}
