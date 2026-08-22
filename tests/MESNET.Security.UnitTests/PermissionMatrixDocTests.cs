using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// ADR-0002'deki izin matrisinin <b>koda uygunluk</b> kilidi.
///
/// <para><b>Neden test:</b> ADR "elimizdeki referans kaynak" olarak yazıldı. Elle tutulan bir
/// matris ilk yeni izinde çürür ve çürüdüğü fark edilmez — doküman referans sayıldığı için
/// yanlış hâli doğru sanılır ve bir sonraki önek kararı yanlış tabloya bakılarak verilir.</para>
///
/// <para>Test sapmayı kırmızıya çevirir ve doğru metni dosyaya yazar; düzeltme kopyala-yapıştır.</para>
/// </summary>
public sealed class PermissionMatrixDocTests
{
    private const string AdrRelativePath = "src/Docs/docs/architecture/adr-0002-izin-agaci-ve-onek-secimi.md";

    [Fact]
    public void ADR_izin_matrisi_kodla_ayni()
    {
        var adrPath = Path.Combine(RepositoryRoot(), AdrRelativePath);
        File.Exists(adrPath).ShouldBeTrue($"ADR bulunamadı: {adrPath}");

        var content = File.ReadAllText(adrPath).ReplaceLineEndings("\n");
        var expected = PermissionMatrixDoc.Build().ReplaceLineEndings("\n");

        var start = content.IndexOf(PermissionMatrixDoc.BeginMarker, StringComparison.Ordinal);
        var end = content.IndexOf(PermissionMatrixDoc.EndMarker, StringComparison.Ordinal);

        start.ShouldBeGreaterThanOrEqualTo(0, "ADR'de başlangıç işaretçisi yok.");
        end.ShouldBeGreaterThan(start, "ADR'de bitiş işaretçisi yok ya da sırası ters.");

        var actual = content[start..(end + PermissionMatrixDoc.EndMarker.Length)];

        if (actual == expected) return;

        // Sapma varsa doğru metni diske yaz — düzeltme kopyala-yapıştır olsun, elle yeniden
        // yazılmasın (elle yazım bu testin çözdüğü sorunun ta kendisi).
        var regeneratedPath = Path.Combine(AppContext.BaseDirectory, "permission-matrix.generated.md");
        File.WriteAllText(regeneratedPath, expected);

        Assert.Fail(
            $"ADR-0002'deki izin matrisi kodla uyuşmuyor.\n" +
            $"Üretilmiş doğru metin: {regeneratedPath}\n" +
            $"Bu dosyanın içeriğini {AdrRelativePath} dosyasındaki işaretçiler arasına yapıştırın.");
    }

    /// <summary>
    /// Depo kökünü çözer. Test çıktısı derin bir <c>bin/</c> altında olduğu için göreli yol
    /// doğrudan kullanılamaz; çözüm dosyası (<c>MESNET.slnx</c>) işaretçi olarak aranır.
    /// </summary>
    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Depo kökü bulunamadı (MESNET.slnx aranıyordu): {AppContext.BaseDirectory}");
    }
}
