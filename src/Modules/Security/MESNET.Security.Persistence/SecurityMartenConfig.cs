using Marten;
using MESNET.Security.Core.Entities;

namespace MESNET.Security.Persistence;

public class SecurityMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<UserAccount>().DatabaseSchemaName("security");
        options.Schema.For<UserAccount>().Index(x => x.KeycloakUserId);
        options.Schema.For<UserAccount>().Index(x => x.Username);
        options.Schema.For<UserAccount>().Index(x => x.Email);
        options.Schema.For<UserAccount>().Index(x => x.InstitutionId);

        options.Schema.For<UserInvitation>().DatabaseSchemaName("security");
        options.Schema.For<UserInvitation>().Index(x => x.Email);
        options.Schema.For<UserInvitation>().Index(x => x.Status);
        options.Schema.For<UserInvitation>().Index(x => x.InstitutionId);
    }
}
