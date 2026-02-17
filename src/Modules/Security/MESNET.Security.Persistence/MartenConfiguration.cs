using Marten;

namespace MESNET.Security.Persistence;

public static class MartenConfiguration
{
    public static void ConfigureSecuritySchema(this StoreOptions options)
    {
        // Schema configuration is handled by SecurityMartenConfig (IConfigureMarten)
    }
}
