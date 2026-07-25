namespace MESNET.Payment.Core.ReadModels;

/// <summary>
/// Payment modülünün yerel işletme profili — Business.BusinessRegistered/BusinessUpdated
/// olaylarından beslenir.
/// </summary>
/// <remarks>
/// 3308 Madde 25 taban ücret oranı işletmenin personel sayısına bağlı (eşiğin altı %15, üstü %30).
/// Modüller arası doğrudan DB erişimi yasak olduğu için bu bilgi olayla taşınıp burada
/// denormalize tutulur (#64).
/// </remarks>
public class BusinessPaymentProfile
{
    public Guid Id { get; set; }       // BusinessId
    public string Name { get; set; } = "";
    public int PersonnelCount { get; set; }
}
