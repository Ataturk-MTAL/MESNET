using MESNET.Common.Shared;

namespace MESNET.Business.Application.Commands;

public sealed record RegisterBusiness(
    string Name,
    /// <summary>
    /// Vergi kimliği — 10 haneli VKN ya da 11 haneli TCKN (#150). Paylaşımlı kataloğun
    /// <b>doğal anahtarıdır</b>: aynı firmayı iki okulun ayrı ayrı kaydetmesini engelleyen
    /// tek alan budur.
    /// </summary>
    string TaxNumber,
    string Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    int PersonnelCount,
    Location? Location,
    int TotalSlots,
    List<string>? Sectors,
    /// <summary>
    /// Kamu kurum/kuruluşu mu (#157). 3308 Geçici Madde 12 gereği kamu kurumlarına devlet
    /// katkısı ödenmez; bu bilgi kayıt anında girilir, sistem türetemez.
    /// </summary>
    bool IsPublicInstitution = false);
