using Marten;
using MESNET.Institution.Core.Entities;

namespace MESNET.Institution.Persistence;

public class InstitutionMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<Core.Entities.Institution>().DatabaseSchemaName("institution");
        options.Schema.For<FieldOfStudy>().DatabaseSchemaName("institution");
        options.Schema.For<SystemParameter>().DatabaseSchemaName("institution");
    }
}
