using Marten;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.ReadModels;

namespace MESNET.Institution.Persistence;

public class InstitutionMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<Core.Entities.Institution>().DatabaseSchemaName("institution");
        options.Schema.For<FieldOfStudy>().DatabaseSchemaName("institution");
        options.Schema.For<SystemParameter>().DatabaseSchemaName("institution");
        options.Schema.For<AcademicPeriod>().DatabaseSchemaName("institution");
        options.Schema.For<AcademicPeriod>().Index(x => x.InstitutionId);

        // UserNameView (Security.UserDisplayNameUpserted ile beslenir) — denetim alanları
        // yalnız kullanıcı kimliğini saklar, ad sorgu tarafında buradan çözülür (#137)
        options.Schema.For<UserNameView>().DatabaseSchemaName("institution");
    }
}
