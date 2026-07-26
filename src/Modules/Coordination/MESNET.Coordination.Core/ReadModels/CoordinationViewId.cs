using System.Security.Cryptography;
using System.Text;

namespace MESNET.Coordination.Core.ReadModels;

/// <summary>
/// <see cref="BusinessCoordinationView"/> kimliği <c>(BusinessId, BranchCode, AcademicPeriodId)</c>
/// üçlüsünden <b>deterministik</b> üretilir: aynı üçlü her zaman aynı Guid'i verir.
///
/// Neden: aynı işletmeye birden çok alandan öğrenci yerleşebilir ve her alan kendi
/// koordinatörlük satırını taşır. Deterministik kimlik sayesinde consumer'lar ve handler'lar
/// satırı ek sorgu yapmadan <c>LoadAsync</c> ile bulur, olay yeniden oynatıldığında da
/// aynı satır güncellenir (idempotent).
/// </summary>
public static class CoordinationViewId
{
    /// <summary>
    /// İşletme düzeyi (alan-bağımsız) "temel satır"ın alan kodu. Bu satır işletmenin
    /// adres/konum/mesafe gibi ortak bilgilerini taşır; öğrenci sayısı veya atama taşımaz.
    /// </summary>
    public const string BaseBranchCode = "";

    /// <summary>İşletme düzeyi temel satırın kimliği.</summary>
    public static Guid Base(Guid businessId) =>
        For(businessId, BaseBranchCode, Guid.Empty);

    /// <summary>Alan satırının kimliği — üçlünün stabil SHA-256 türevi.</summary>
    public static Guid For(Guid businessId, string? branchCode, Guid academicPeriodId)
    {
        // Alan kodu büyük/küçük harf ve boşluk farkından etkilenmemeli — yalnız hash girdisi
        // normalize edilir, satırda saklanan değer olduğu gibi kalır.
        var normalizedBranch = (branchCode ?? string.Empty).Trim().ToUpperInvariant();
        var payload = $"{businessId:N}|{normalizedBranch}|{academicPeriodId:N}";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));

        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);

        // RFC 4122 sürüm (8 = özel/isimden türetilmiş) ve varyant bitleri — üretilen değer
        // geçerli biçimli bir UUID olsun diye. Determinizmi bozmaz.
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

        return new Guid(bytes, bigEndian: true);
    }
}
