using JasperFx.Events.Projections;
using Marten;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.ReadModels;

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

        options.Schema.For<AcademicPeriodView>().DatabaseSchemaName("contract");
        options.Schema.For<AcademicPeriodView>().Index(x => x.InstitutionId);

        // Öğrenci ad/numara araması için denormalize view (Enrollment.StudentRegistered ile beslenir)
        options.Schema.For<StudentNameView>().DatabaseSchemaName("contract");
        options.Schema.For<StudentNameView>().Index(x => x.InstitutionId);
    }
}
