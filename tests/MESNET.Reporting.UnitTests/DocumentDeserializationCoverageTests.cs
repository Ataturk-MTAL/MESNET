using System.Reflection;
using MESNET.Reporting.Core.Enums;
using Shouldly;
using Xunit;

namespace MESNET.Reporting.UnitTests;

/// <summary>
/// Her <see cref="MebFormType"/> üyesinin <b>talep üzerine yeniden üretim</b> dalı olmalı (#267).
///
/// <para><b>Bulunan hata:</b> Form 8 (Dönem Not Fişi, #60 ile eklenmiş) için
/// <c>DocumentQueryHandler.DeserializeToDocument</c>'ta dal yoktu; metot sona düşüp
/// <c>InvalidOperationException</c> fırlatıyordu.</para>
///
/// <para><b>Neden gizli kaldı:</b> <c>GET /api/reports/documents/{id}/pdf</c> iki yollu —
/// <c>PdfStoragePath</c> doluysa MinIO presigned URL döner (çalışır), boşsa <b>ya da presigned
/// çağrısı başarısız olursa</b> PDF <c>FormDataJson</c>'dan sıfırdan üretilir (patlar). MinIO
/// erişilebilir olduğu sürece hata görünmüyor; nesne silinir, bucket taşınır ya da presigned
/// çağrısı düşerse belge indirilemez hâle geliyor. Kalıcı olan aslında <c>FormDataJson</c>'dur;
/// MinIO snapshot'ı bir kolaylıktır.</para>
///
/// <para><b>Asıl düzeltme bu testtir:</b> tek satır eklemek bugünkü boşluğu kapatır, ama yeni
/// form tipi eklendiğinde aynı şey sessizce tekrarlanırdı — nitekim tekrarlanmıştı.</para>
/// </summary>
public sealed class DocumentDeserializationCoverageTests
{
    /// <summary>
    /// Kaynak taraması: <c>DeserializeToDocument</c> gövdesinde her form tipinin adı geçmeli.
    ///
    /// <para>Metot private ve <c>IDocument</c> üretmek için gerçek JSON gerektiriyor; reflection
    /// ile çağırmak her form tipi için geçerli bir <c>FormData</c> kurmayı gerektirirdi. Kapsam
    /// sorusu için kaynak taraması yeterli ve kırılgan değil.</para>
    /// </summary>
    [Fact]
    public void Her_form_tipinin_yeniden_uretim_dali_var()
    {
        var body = DeserializeMethodBody();

        var missing = MebFormType.List
            .Where(t => !body.Contains($"MebFormType.{t.Name}", StringComparison.Ordinal))
            .Select(t => $"{t.Name} ({t.Value})")
            .ToList();

        missing.ShouldBeEmpty(
            "Bu form tipleri talep üzerine yeniden üretilemez ve indirme MinIO erişilemez "
            + "olduğunda InvalidOperationException ile patlar. DeserializeToDocument'a dal "
            + $"ekleyin. Eksik: {string.Join(", ", missing)}");
    }

    /// <summary>Tarama gerçekten bir gövde okuyor olmalı — boş metin her şeyi yanlış yeşil yapar.</summary>
    [Fact]
    public void Tarama_gercekten_govde_okuyor()
    {
        DeserializeMethodBody().ShouldContain("MebFormType.InternshipContract");
        MebFormType.List.Count.ShouldBeGreaterThan(1);
    }

    private static string DeserializeMethodBody()
    {
        var source = File.ReadAllText(HandlerSourcePath());

        var start = source.IndexOf("private static IDocument DeserializeToDocument", StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0, "DeserializeToDocument bulunamadı — test bakımsız kalmış.");

        var end = source.IndexOf("Bilinmeyen form tipi", start, StringComparison.Ordinal);
        end.ShouldBeGreaterThan(start, "Metodun sonu bulunamadı.");

        return source[start..end];
    }

    private static string HandlerSourcePath() => Path.Combine(RepoRoot(),
        "src/Modules/Reporting/MESNET.Reporting.Application/Handlers/DocumentQueryHandler.cs");

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx).");
    }
}
