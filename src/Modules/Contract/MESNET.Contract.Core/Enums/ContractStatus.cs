using Ardalis.SmartEnum;

namespace MESNET.Contract.Core.Enums;

public sealed class ContractStatus : SmartEnum<ContractStatus>
{
    public static readonly ContractStatus Draft                = new(nameof(Draft),                1, "Taslak");
    public static readonly ContractStatus AwaitingSignature    = new(nameof(AwaitingSignature),    2, "İmza Bekliyor");
    public static readonly ContractStatus Active               = new(nameof(Active),               3, "Aktif");
    public static readonly ContractStatus Suspended            = new(nameof(Suspended),            4, "Askıda");
    public static readonly ContractStatus TerminationRequested = new(nameof(TerminationRequested), 5, "Fesih Talep Edildi");
    public static readonly ContractStatus Terminated           = new(nameof(Terminated),           6, "Feshedilmiş");
    public static readonly ContractStatus Completed            = new(nameof(Completed),            7, "Tamamlanmış");

    public string Slug { get; }

    private ContractStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public bool IsFinal => this == Terminated || this == Completed;

    private static readonly Dictionary<ContractStatus, HashSet<ContractStatus>> Transitions = new()
    {
        [Draft]                = [AwaitingSignature],
        [AwaitingSignature]    = [Active],
        [Active]               = [Suspended, Terminated, Completed, TerminationRequested],
        [Suspended]            = [Active, Terminated, TerminationRequested],
        [TerminationRequested] = [Terminated, Active],  // Active = talep reddedildi
        [Terminated]           = [],
        [Completed]            = []
    };

    public bool CanTransitionTo(ContractStatus target)
        => Transitions.TryGetValue(this, out var allowed) && allowed.Contains(target);
}
