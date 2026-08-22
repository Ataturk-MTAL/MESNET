namespace MESNET.Reporting.Core.ReadModels;

/// <summary>
/// Reporting modülünün yerel işletme iletişim/yetkili read-model'i — Business modülünün
/// <c>BusinessRegistered</c> / <c>BusinessUpdated</c> olaylarından beslenir.
/// Öğrenci yerleştirmesinden bağımsızdır: işletme olayı, yerleştirme oluşmadan önce gelse de
/// bilgi burada saklanır ve belge üretiminde işletme kimliğiyle okunur (#99).
/// </summary>
public class BusinessContactReportView
{
    /// <summary>Business modülündeki işletme kimliği (document id olarak kullanılır).</summary>
    public Guid Id { get; set; }

    public string BusinessName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    /// <summary>İşletme Yetkilisi — Form 8 imza bloğu (BusinessRepresentative.FullName).</summary>
    public string? RepresentativeName { get; set; }

    /// <summary>Usta Öğretici / Eğitici Personel adı.</summary>
    public string? MasterInstructorName { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
