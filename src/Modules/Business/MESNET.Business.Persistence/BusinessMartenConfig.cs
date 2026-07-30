using Marten;
using MESNET.Business.Core.ReadModels;

namespace MESNET.Business.Persistence;

public class BusinessMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<Core.Entities.Business>().DatabaseSchemaName("business");

        // Denormalize read models — Institution event'lerinden
        options.Schema.For<InstitutionBranchView>().DatabaseSchemaName("business");
        options.Schema.For<InstitutionBranchView>().Index(x => x.InstitutionId);
        options.Schema.For<InstitutionBranchView>().Index(x => x.IsActive);


        // Denormalize read models — Enrollment event'lerinden
        options.Schema.For<PlacedStudentView>().DatabaseSchemaName("business");
        options.Schema.For<PlacedStudentView>().Index(x => x.BusinessId);
        options.Schema.For<PlacedStudentView>().Index(x => x.IsActive);
    }
}
