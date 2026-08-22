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
        // Vergi kimliği paylaşımlı kataloğun doğal anahtarıdır (#150). Kısıt KISMİDİR:
        // #150 öncesinde kaydedilmiş işletmelerin alanı NULL'dur (ölçüldü: 100/100) ve tam
        // kısıt göçü ilk açılışta düşürürdü. NULL'lar PostgreSQL'de zaten benzersizlik
        // saymaz; predicate niyeti AÇIK yazar.
        //
        // İsim kısa verildi — PostgreSQL 64 karakter sınırı (CLAUDE.md composite index kuralı).
        options.Schema.For<Core.Entities.Business>()
            .Index(x => x.TaxNumber, x =>
            {
                x.IsUnique = true;
                x.Name = "idx_business_taxno_uniq";
                x.Predicate = "data ->> 'taxNumber' is not null";
            });

        options.Schema.For<InstitutionBranchView>().Index(x => x.InstitutionId);
        options.Schema.For<InstitutionBranchView>().Index(x => x.IsActive);


        // Denormalize read models — Enrollment event'lerinden
        options.Schema.For<PlacedStudentView>().DatabaseSchemaName("business");
        options.Schema.For<PlacedStudentView>().Index(x => x.BusinessId);
        options.Schema.For<PlacedStudentView>().Index(x => x.IsActive);
    }
}
