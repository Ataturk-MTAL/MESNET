using MESNET.Common.Infrastructure.Tenancy;
using Npgsql;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Row-level security ihlali genel 500'lerin arasında kaybolmamalı (#318, kural 7).
///
/// <para>Bu iki hata normal akışta HİÇ oluşmamalı: WITH CHECK ihlali (başka okulun damgasıyla
/// yazma) ve kiracı ayarı olmadan kiracılı tabloya dokunmak. Oluşuyorsa ya bir hata (kiracısız
/// kod yolu) ya da bir saldırı girişimidir; ikisi de alarm ister.</para>
/// </summary>
public sealed class RlsViolationClassifierTests
{
    private static PostgresException Pg(string sqlState, string message) =>
        new(message, "ERROR", "ERROR", sqlState);

    [Fact]
    public void Baska_kiraci_adina_yazma_ihlal_sayilir()
    {
        RlsViolationClassifier.Classify(
                Pg("42501", "new row violates row-level security policy for table \"mt_doc_teacherprofile\""))
            .ShouldNotBeNull();
    }

    [Fact]
    public void Kiraci_ayari_olmadan_erisim_ihlal_sayilir()
    {
        RlsViolationClassifier.Classify(
                Pg("42704", $"unrecognized configuration parameter \"{TenantRls.SettingName}\""))
            .ShouldNotBeNull();
    }

    /// <summary>Wolverine/Marten istisnayı sarabilir — iç zincir yürünür.</summary>
    [Fact]
    public void Sarilmis_istisnada_da_bulunur()
    {
        var wrapped = new InvalidOperationException("dış",
            new Exception("ara", Pg("42501", "new row violates row-level security policy for table \"x\"")));

        RlsViolationClassifier.Classify(wrapped).ShouldNotBeNull();
    }

    /// <summary>Aynı kodlu ama RLS'le ilgisiz hatalar alarm üretmemeli — yoksa alarm gürültüye boğulur.</summary>
    [Theory]
    [InlineData("42501", "permission denied for table mt_doc_x")]
    [InlineData("42704", "unrecognized configuration parameter \"statement_timeout_x\"")]
    [InlineData("23505", "duplicate key value violates unique constraint")]
    public void RLS_disi_hatalar_ihlal_sayilmaz(string sqlState, string message)
    {
        RlsViolationClassifier.Classify(Pg(sqlState, message)).ShouldBeNull();
    }

    [Fact]
    public void Postgres_disi_istisna_ihlal_sayilmaz()
    {
        RlsViolationClassifier.Classify(new InvalidOperationException("x")).ShouldBeNull();
        RlsViolationClassifier.Classify(null).ShouldBeNull();
    }
}
