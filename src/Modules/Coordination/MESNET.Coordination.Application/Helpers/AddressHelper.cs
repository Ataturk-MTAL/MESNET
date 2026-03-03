namespace MESNET.Coordination.Application.Helpers;

public static class AddressHelper
{
    /// <summary>
    /// Adres stringinden ilçe bilgisini çıkarır.
    /// Format: "Mahalle, İlçe, İl" → "İlçe"
    /// </summary>
    public static string? ExtractDistrict(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var parts = address.Split(',', StringSplitOptions.TrimEntries);
        return parts.Length >= 2 ? parts[^2] : null;
    }
}
