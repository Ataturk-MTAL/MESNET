using Marten;
using MESNET.Security.Core.Entities;
using MESNET.Security.Core.ReadModels;

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

        // Rol → atanabilir yetki domain kapsamı (singleton, yapılandırılabilir)
        options.Schema.For<PermissionScopeConfig>().DatabaseSchemaName("security");

        // Veli bağı eksik ölçümü (#271) — Enrollment'ın StudentRegistered olayından beslenir.
        options.Schema.For<GuardianLinkView>().DatabaseSchemaName("security");
        options.Schema.For<GuardianLinkView>().Index(x => x.InstitutionId);
    }
}
