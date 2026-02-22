using MESNET.Business.Core.Enums;
using MESNET.Common.Shared;

namespace MESNET.Business.Shared.Events;

public sealed record BusinessRegistered(
    Guid BusinessId,
    string Name,
    Location? Location,
    RegistrationSource Source,
    int TotalSlots = 0);
