namespace MESNET.Common.Shared.Security;

/// <summary>
/// Kurum ağacındaki materyalize yolun <b>tek</b> biçim otoritesi.
///
/// <para><b>Neden materyalize yol:</b> alt ağaç sorgusu <c>Path.StartsWith(aktörünYolu)</c>
/// olur ve Marten bunu <c>LIKE 'önek%'</c> çevirir. Ham SQL, <c>WITH RECURSIVE</c> ve her
/// istekte ağaç yürüyüşü gerekmez.</para>
///
/// <para><b>Neden sondaki ayraç biçimin parçası:</b> onsuz <c>/33/1</c> öneki <c>/33/10...</c>
/// yolunu da yakalar ve bir ilçe yetkilisi kardeş ilçeyi görür. Kimlikler Guid olduğu için
/// segmentler bugün sabit uzunluktadır ve çakışma oluşamaz; garanti yine de kimlik tipine
/// bırakılmaz.</para>
///
/// <para><b>Yol kimliklerden kurulur, adlardan DEĞİL.</b> İlçe adı düzeltildiğinde yolun
/// bozulmaması gerekir.</para>
/// </summary>
public static class InstitutionPath
{
    public const char Separator = '/';

    /// <summary>Kök (il) düğümünün yolu.</summary>
    public static string Root(Guid nodeId) => $"{Separator}{nodeId:D}{Separator}";

    /// <summary>Üst düğümün yoluna bir segment ekler.</summary>
    /// <exception cref="ArgumentException">Üst yol boş — kök için <see cref="Root"/> kullanın.</exception>
    public static string Child(string parentPath, Guid nodeId)
    {
        var normalized = Normalize(parentPath)
            ?? throw new ArgumentException(
                "Üst düğümün yolu boş olamaz; kök düğüm için Root(...) kullanın.", nameof(parentPath));

        return $"{normalized}{nodeId:D}{Separator}";
    }

    /// <summary>
    /// Baştaki ve sondaki ayracı garanti eder. Boş/boşluk girdi <c>null</c> döner —
    /// "yol yok" ile "kök yolu" birbirine karışmamalıdır.
    /// </summary>
    public static string? Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        var trimmed = path.Trim();

        if (trimmed[0] != Separator) trimmed = Separator + trimmed;
        if (trimmed[^1] != Separator) trimmed += Separator;

        return trimmed;
    }

    /// <summary>
    /// <paramref name="descendantPath"/>, <paramref name="ancestorPath"/>'in alt ağacında mı?
    /// Düğüm kendi alt ağacındadır; ÜST düğüm ve kardeşler değildir.
    /// </summary>
    public static bool Contains(string? ancestorPath, string? descendantPath)
    {
        if (Normalize(ancestorPath) is not { } ancestor) return false;
        if (Normalize(descendantPath) is not { } descendant) return false;

        return descendant.StartsWith(ancestor, StringComparison.Ordinal);
    }
}
