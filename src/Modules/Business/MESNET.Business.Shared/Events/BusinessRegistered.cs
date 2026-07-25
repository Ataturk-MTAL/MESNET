using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessRegistered(
    Guid BusinessId,
    Guid TenantId,
    string Name,
    string? Address,
    Location? Location,
    // Modüller arası event: SmartEnum yerine Name string'i taşınır (RegistrationSource.Name)
    string Source,
    int TotalSlots = 0,
    List<string>? Sectors = null,
    string? PhoneNumber = null,
    string? Email = null,
    string? MasterInstructorName = null,
    // 3308 Madde 25: staj ücreti işletmenin personel sayısına göre değişiyor (20 altı/üstü).
    // Payment modülü bu bilgiyi başka modülün şemasından okuyamaz, olayla taşınmalı (#64).
    int PersonnelCount = 0);
