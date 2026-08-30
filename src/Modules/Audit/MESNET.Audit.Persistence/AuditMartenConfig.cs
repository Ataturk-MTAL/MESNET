using Marten;
using MESNET.Audit.Core.Entities;

namespace MESNET.Audit.Persistence;

public class AuditMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Schema.For<AuditEntry>().DatabaseSchemaName("audit");

        // İsimler ELLE verilir: PostgreSQL tanımlayıcı sınırı 64 karakter ve Marten'in
        // otomatik adı (mt_doc_auditentry_idx_...) uzun alan adlarıyla bunu aşar.
        options.Schema.For<AuditEntry>()
            .Index(x => x.OccurredAt, x => x.Name = "idx_audit_occurred");
        options.Schema.For<AuditEntry>()
            .Index(x => x.ActorId, x => x.Name = "idx_audit_actor");
        // Yol öneki sorgusu (StartsWith → LIKE 'önek%'). Düz btree'dir; PostgreSQL bunu önek
        // araması için ancak C collation ya da text_pattern_ops opclass'ıyla kullanır.
        // Aynı not Institution.Path indeksinde de duruyor — bedel aynı ölçekte ölçülemez.
        options.Schema.For<AuditEntry>()
            .Index(x => x.SubjectInstitutionPath, x => x.Name = "idx_audit_subject_path");
    }
}
