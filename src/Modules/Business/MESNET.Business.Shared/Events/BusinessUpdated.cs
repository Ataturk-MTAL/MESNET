using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessUpdated(
    Guid BusinessId,
    string Name,
    string? Address,
    Location? Location,
    List<string>? Sectors = null,
    string? PhoneNumber = null,
    string? Email = null,
    string? MasterInstructorName = null,
    // Personel sayısı değişince staj ücreti oranı da değişebilir (20 altı %15, üstü %30) — #64.
    int PersonnelCount = 0);
