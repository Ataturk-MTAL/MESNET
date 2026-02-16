using Marten;
using MESNET.Internship.Application.Sagas;
using MESNET.Internship.Core.Entities;

namespace MESNET.Internship.Persistence;

public class InternshipMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        // Saga state — Wolverine otomatik yönetir, sadece schema belirt
        options.Schema.For<InternshipSaga>().DatabaseSchemaName("internship");

        // InternshipSummary — document storage (consumer'lar günceller)
        options.Schema.For<InternshipSummary>().DatabaseSchemaName("internship");
        options.Schema.For<InternshipSummary>().Index(x => x.StudentId);
        options.Schema.For<InternshipSummary>().Index(x => x.BusinessId);
        options.Schema.For<InternshipSummary>().Index(x => x.InstitutionId);
    }
}
