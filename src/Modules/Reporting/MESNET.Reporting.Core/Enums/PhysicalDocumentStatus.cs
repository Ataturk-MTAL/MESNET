using Ardalis.SmartEnum;

namespace MESNET.Reporting.Core.Enums;

/// <summary>
/// Fiziksel doküman yaşam döngüsü durumları.
/// Sadece ileri yönlü geçiş yapılabilir.
/// </summary>
public sealed class PhysicalDocumentStatus : SmartEnum<PhysicalDocumentStatus>
{
    public static readonly PhysicalDocumentStatus Generated = new(nameof(Generated), 1, "Oluşturuldu");
    public static readonly PhysicalDocumentStatus Printed = new(nameof(Printed), 2, "Yazdırıldı");
    public static readonly PhysicalDocumentStatus SignedAndReturned = new(nameof(SignedAndReturned), 3, "İmzalanıp Teslim Edildi");
    public static readonly PhysicalDocumentStatus Archived = new(nameof(Archived), 4, "Arşivlendi");

    private PhysicalDocumentStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public string Slug { get; }

    /// <summary>
    /// Belirtilen duruma geçiş yapılabilir mi kontrol eder.
    /// Sadece bir sonraki duruma geçişe izin verilir.
    /// </summary>
    public bool CanTransitionTo(PhysicalDocumentStatus target)
        => target.Value == Value + 1;
}
