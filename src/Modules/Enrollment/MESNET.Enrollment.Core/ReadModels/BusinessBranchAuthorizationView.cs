namespace MESNET.Enrollment.Core.ReadModels;

/// <summary>
/// İşletmenin öğrenci alabileceği alanların Enrollment kopyası (#119).
///
/// Business modülünün <c>BusinessBranchesAuthorized</c> olayından beslenir. Enrollment,
/// Business'ın Core'unu okuyamaz (şema izolasyonu) — yerleştirme guard'ı bu read-model'e bakar.
/// Kayıt yoksa veya liste boşsa işletme hiçbir alandan öğrenci alamaz.
/// </summary>
public class BusinessBranchAuthorizationView
{
    /// <summary>İşletme kimliği (BusinessId).</summary>
    public Guid Id { get; set; }

    public string BusinessName { get; set; } = "";

    /// <summary>Yalnız aktif (iptal edilmemiş) alan kodları.</summary>
    public List<string> ActiveBranchCodes { get; set; } = [];

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
