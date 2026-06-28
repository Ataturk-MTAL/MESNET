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
    string? MasterInstructorName = null);
