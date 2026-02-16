using Ardalis.SmartEnum;

namespace MESNET.Business.Core.Enums;

public sealed class RegistrationSource : SmartEnum<RegistrationSource>
{
    public static readonly RegistrationSource InstitutionRegistered = new(nameof(InstitutionRegistered), 1, "Kurum Kaydı", requiresApproval: false);
    public static readonly RegistrationSource SelfRegistered = new(nameof(SelfRegistered), 2, "İşletme Kendi Kaydı", requiresApproval: true);

    public string Slug { get; }
    public bool RequiresApproval { get; }

    private RegistrationSource(string name, int value, string slug, bool requiresApproval) : base(name, value)
    {
        Slug = slug;
        RequiresApproval = requiresApproval;
    }
}
