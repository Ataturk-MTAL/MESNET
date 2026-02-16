using Marten;
using Marten.Events.Projections;
using MESNET.Contract.Core.Aggregates;

namespace MESNET.Contract.Persistence;

public class ContractMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        // Inline snapshot — aggregate her event sonrası otomatik güncellenir
        options.Projections.Snapshot<InternshipContract>(SnapshotLifecycle.Inline);

        // Snapshot document schema + index'ler
        options.Schema.For<InternshipContract>().DatabaseSchemaName("contract");
        options.Schema.For<InternshipContract>().Index(x => x.StudentId);
        options.Schema.For<InternshipContract>().Index(x => x.BusinessId);
        options.Schema.For<InternshipContract>().Index(x => x.InstitutionId);
    }
}
