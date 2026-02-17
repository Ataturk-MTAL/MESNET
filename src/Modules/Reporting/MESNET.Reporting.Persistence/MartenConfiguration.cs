using Marten;
using MESNET.Reporting.Core.Entities;

namespace MESNET.Reporting.Persistence;

public class ReportingMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        var schema = options.Schema.For<GeneratedDocument>();
        schema.DatabaseSchemaName("reporting");

        // Tekil sorgular
        schema.Index(x => x.Status);
        schema.Index(x => x.FormType);
        schema.Index(x => x.GeneratedAt);

        // İlişkili entity ID'leri
        schema.Index(x => x.StudentId);
        schema.Index(x => x.BusinessId);
        schema.Index(x => x.InstitutionId);
        schema.Index(x => x.TeacherId);

        // Composite index'ler — Müdür Yardımcısı bekleyen dokümanları görmek için
        schema.Index(x => new { x.TeacherId, x.Status });
        schema.Index(x => new { x.InstitutionId, x.Status });
    }
}
