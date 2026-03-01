namespace MESNET.Common.Shared.Pagination;

/// <summary>
/// Tüm sayfalı sorgu record'larının base tipi.
/// Query record'lar bu record'dan inherit eder ve kendi filtre parametrelerini ekler.
/// Endpoint'ler querystring'den page/pageSize/sortBy/descending/search parametrelerini alır.
/// </summary>
public abstract record PagedQuery
{
    /// <summary>1-tabanlı sayfa numarası. Varsayılan: 1</summary>
    public int Page { get; init; } = 1;

    /// <summary>Sayfa başına kayıt sayısı. Varsayılan: 20, Maks: 100</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>Sıralama alanı (camelCase, entity property adıyla eşleşir)</summary>
    public string? SortBy { get; init; }

    /// <summary>Azalan sıralama. true = descending, false = ascending</summary>
    public bool Descending { get; init; }

    /// <summary>Serbest metin arama terimi. Modül bazlı aranabilir alanlara uygulanır.</summary>
    public string? Search { get; init; }

    /// <summary>Doğrulanmış sayfa numarası (min 1)</summary>
    public int SafePage => Math.Max(1, Page);

    /// <summary>Doğrulanmış sayfa boyutu (min 1, max 100)</summary>
    public int SafePageSize => Math.Clamp(PageSize, 1, 100);

    /// <summary>LINQ Skip değeri</summary>
    public int Skip => (SafePage - 1) * SafePageSize;
}
