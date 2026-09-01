using MESNET.Audit.Application.Auditing;
using MESNET.Audit.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// <b>Ölçülmüş boş kilit — AuditWriter dikişi (B parçası, son inceleme madde 5).</b>
///
/// <para><b>Neden bu test var — ölçüldü:</b> <c>AuditWriter.WriteAsync</c> içinde
/// <c>context.ActiveInstitutionId</c> → <c>null</c> yapıldığında (iki çağrı sitesi:
/// <see cref="AuditWriter.ResolveSubjectFor"/> ve <see cref="AuditWriter.BuildInput"/>)
/// <c>SubjectInstitutionId</c> aktörün EV kurumuna düşüyor ve <c>CrossedTenantBoundary</c>
/// HER ZAMAN <c>false</c> oluyor — spec'in "B'nin izli verilmesinin TEK sebebi ortadan
/// kalkar" dediği hâl. Audit modülünün 49/49 testi bu regresyonu YAKALAMIYORDU:
/// <c>AuditEntryFactoryTests</c> yalnız <see cref="AuditEntryFactory"/>'yi elle kurulmuş bir
/// <c>AuditInput</c> ile test ediyor (AuditWriter'ın <c>AuditContext</c>'ten <c>AuditInput</c>
/// KURMA mantığına hiç değmiyor); <c>AuditMiddlewareContractTests</c>'teki sahte yazıcı
/// (<c>SahteYazici</c>) ise ham <c>AuditContext</c>'i saklıyor, hiçbir satır KURMUYOR.</para>
///
/// <para><b>Neden gerçek AuditWriter kodu, sahte kopya değil:</b> <c>AuditWriter.WriteAsync</c>
/// Marten <c>IDocumentStore</c> gerektirir ve bu depoda UnitTests projeleri Postgres'e
/// bağlanmaz. Brief'in izin verdiği ikinci seçenek uygulandı: <c>AuditContext</c> →
/// <c>AuditInput</c>/konu-kurum eşlemesi <see cref="AuditWriter.ResolveSubjectFor"/> ve
/// <see cref="AuditWriter.BuildInput"/> adıyla SAF, <c>public static</c> metotlara çıkarıldı
/// (Marten/pathLookup çağrılarının DIŞINDA kalan kısım). <c>WriteAsync</c> artık BU İKİ
/// METODU çağırıyor — üretim davranışı DEĞİŞMEDİ, yalnız test edilebilir hâle geldi. Bu test
/// o iki metodu ÜZERİNDEN gerçek üretim eşlemesini koşturur ve sonuçtaki
/// <c>AuditEntry.CrossedTenantBoundary</c>'i doğrular.</para>
/// </summary>
public sealed class AuditWriterActiveContextMappingTests
{
    private static readonly Guid AktorKurumu = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid AktifBaglam = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static AuditContext BaglamKur(Guid? activeInstitutionId) => new()
    {
        ActorId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        ActorName = "Ayşe İl Yetkilisi",
        CommandType = typeof(object),
        Command = null,
        TenantId = AktifBaglam.ToString(),
        ActorInstitutionId = AktorKurumu,
        ActorInstitutionPath = "/il/",
        ActiveInstitutionId = activeInstitutionId,
    };

    /// <summary>
    /// Üretim <c>WriteAsync</c>'in yaptığı SIRAYLA aynı iki adımı (ResolveSubjectFor →
    /// BuildInput → AuditEntryFactory.Succeeded) çağırıp gerçek bir <c>AuditEntry</c> kurar.
    /// </summary>
    private static (Guid? SubjectInstitutionId, bool CrossedTenantBoundary) SatirKur(
        AuditContext context)
    {
        var (subjectId, crossed) = AuditWriter.ResolveSubjectFor(context);
        var input = AuditWriter.BuildInput(context, subjectPathOverride: null);
        var entry = AuditEntryFactory.Succeeded(input);

        // İki bağımsız yol AYNI sonucu vermeli: ResolveSubjectFor'un döndürdüğü değer ile
        // BuildInput → AuditEntryFactory.Succeeded'in İÇİNDE yeniden hesapladığı değer.
        entry.SubjectInstitutionId.ShouldBe(subjectId);
        entry.CrossedTenantBoundary.ShouldBe(crossed);

        return (entry.SubjectInstitutionId, entry.CrossedTenantBoundary);
    }

    [Fact]
    public void Aktif_baglamli_komut_CrossedTenantBoundary_true_uretir()
    {
        var context = BaglamKur(activeInstitutionId: AktifBaglam);

        var (subjectId, crossed) = SatirKur(context);

        subjectId.ShouldBe(AktifBaglam);
        crossed.ShouldBeTrue(
            "Aktif bağlam ev kurumundan farklıysa CrossedTenantBoundary true olmalı — "
            + "B'nin izli verilmesinin tek sebebi budur.");
    }

    [Fact]
    public void Aktif_baglam_yokken_konu_kurum_aktorun_ev_kurumudur()
    {
        var context = BaglamKur(activeInstitutionId: null);

        var (subjectId, crossed) = SatirKur(context);

        subjectId.ShouldBe(AktorKurumu);
        crossed.ShouldBeFalse();
    }
}
