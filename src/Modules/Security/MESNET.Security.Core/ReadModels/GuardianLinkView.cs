namespace MESNET.Security.Core.ReadModels;

/// <summary>
/// Öğrencinin Security tarafındaki yerel görünümü — <b>veli bağı eksiklerini ölçmek</b> için
/// (#271).
///
/// <para><b>Neden gerekli:</b> "hangi öğrencinin velisi bağlı değil" sorusu iki modülü birden
/// ilgilendiriyor — öğrenci listesi Enrollment'ta, bağ (<c>UserAccount.LinkedStudentIds</c>)
/// Security'de. Modüller arası doğrudan sorgu yasak; olay tabanlı yerel görünüm tek meşru yol
/// (<c>StudentNameView</c> ile aynı desen).</para>
///
/// <para><b>Neden ölçmek şart:</b> veli bağı kurulmadan md. 36 (4) tebligatı (#247) veliye
/// <b>hiç ulaşmıyor</b> ve bu sessiz. Kod artık uyarı yazıyor ama log okunmazsa yükümlülük
/// yerine getirilmemiş olur. Sayı görünmeden kimse eksik olduğunu bilmez.</para>
/// </summary>
public class GuardianLinkView
{
    /// <summary>= <c>StudentId</c>.</summary>
    public Guid Id { get; set; }

    /// <summary>Kiracı anahtarı (#147) — öğrenci okulun verisidir.</summary>
    public Guid InstitutionId { get; set; }

    public string FullName { get; set; } = default!;
    public string? StudentNumber { get; set; }
    public string? BranchCode { get; set; }
}
