using System.Text.Json;

namespace MESNET.Seeder.Seeders;

/// <summary>
/// Seeder'ın etkin kiracısı seed ettiği okul mu (#309). Saf fonksiyon — G/Ç yapmaz.
/// </summary>
/// <remarks>
/// <para><b>Neden gerekli:</b> kiracı istekten değil kullanıcı kaydından çözülür (ADR-0003).
/// Seeder'ın hesabı başka bir kuruma bağlıysa (ör. il MEM) okul verisi <b>sessizce</b> o
/// kiracıya düşer; mükerrerlik kontrolleri de aynı kiracıyla süzüldüğü için hiçbir şey görmez
/// ve her koşu yeni kopya üretir. Ölçüldü: 6 öğretmen il MEM kiracısına yazılmıştı.</para>
///
/// <para>Aktif bağlam (il yetkilisinin okulda çalışması) seeder için çözüm değildir: bağlam
/// oturuma (<c>sid</c>) bağlıdır ve password-grant token'ı her tazelemede yeni oturum açar —
/// bağlam koşu ortasında hatasız düşerdi.</para>
/// </remarks>
public static class SeedTenantPolicy
{
    /// <param name="me"><c>GET /api/auth/me</c> yanıtının <c>data</c>'sı.</param>
    /// <param name="schoolId">Seeder'ın kurduğu okul.</param>
    /// <returns>Uyumsuzluğun sebebi; kiracı doğruysa <c>null</c>.</returns>
    public static string? Mismatch(JsonElement me, Guid schoolId)
    {
        var tenantId = me.TryGetProperty("tenantId", out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

        if (Guid.TryParse(tenantId, out var tenant) && tenant == schoolId)
            return null;

        return $"Seeder hesabının kiracısı \"{tenantId ?? "(yok)"}\", seed edilen okul \"{schoolId}\". "
            + "Okul verisi yanlış kiracıya yazılırdı. Hesabı okula bağlayın: "
            + "POST /api/security/users/{id}/institution";
    }
}
