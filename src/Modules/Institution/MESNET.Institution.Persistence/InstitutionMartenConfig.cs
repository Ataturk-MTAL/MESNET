using Marten;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.ReadModels;

namespace MESNET.Institution.Persistence;

public class InstitutionMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<Core.Entities.Institution>().DatabaseSchemaName("institution");

        // Kurum ağacı (#il/ilçe kapsam katmanı). İsimler ELLE verilir: PostgreSQL tanımlayıcı
        // sınırı 64 karakter ve Marten'in otomatik adı (mt_doc_institution_idx_...) bunu aşar.
        //
        // NOT — Path indeksi düz btree'dir. PostgreSQL bunu `LIKE 'önek%'` için ancak C
        // collation ya da text_pattern_ops opclass'ıyla kullanır; varsayılan collation'da
        // planlayıcı seq scan seçebilir. Kurum sayısı (okul + il + ilçe) bu ölçekte üç haneli
        // olduğu için A parçasında bedeli ölçülemez; opclass gerekirse elle DDL ile eklenir.
        options.Schema.For<Core.Entities.Institution>()
            .Index(x => x.Path, x => x.Name = "idx_institution_path");
        // ParentId nullable'dır (Guid?). Marten nullable alanda indeks kurmayı reddederse
        // (sürüm farkı) bu satırı kaldırıp yerine ham DDL koymayın — indeks A parçasında
        // ZORUNLU DEĞİL: ParentId yalnız isteğe bağlı ?parentId= süzgecinde kullanılır ve
        // kurum sayısı üç hanelidir. Kaldırdıysanız gerekçesini buraya yazın.
        options.Schema.For<Core.Entities.Institution>()
            .Index(x => x.ParentId, x => x.Name = "idx_institution_parent");
        options.Schema.For<Core.Entities.Institution>()
            .Index(x => x.NodeTypeName, x => x.Name = "idx_institution_node_type");

        options.Schema.For<FieldOfStudy>().DatabaseSchemaName("institution");
        options.Schema.For<AcademicPeriod>().DatabaseSchemaName("institution");
        options.Schema.For<AcademicPeriod>().Index(x => x.InstitutionId);

        // UserNameView (Security.UserDisplayNameUpserted ile beslenir) — denetim alanları
        // yalnız kullanıcı kimliğini saklar, ad sorgu tarafında buradan çözülür (#137)
        options.Schema.For<UserNameView>().DatabaseSchemaName("institution");
    }
}
