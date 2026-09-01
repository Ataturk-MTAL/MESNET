using System.Text.Json.Serialization;
using MESNET.Audit.Core.Enums;

namespace MESNET.Audit.Core.Entities;

/// <summary>
/// Tek bir yazma komutunun denetim satırı.
/// </summary>
/// <remarks>
/// <para><b>Komut gövdesi SAKLANMAZ.</b> Gövdeler sağlık raporu, maaş ve öğrenci verisi
/// taşır; ize kopyalamak kiracı damgalı belgelerin dışında ikinci bir hassas veri kopyası
/// yaratırdı ve bir silme talebinde iki yerden silmek gerekirdi. "Ne değişti" sorusu olay
/// deposundan (<c>mt_events</c>) cevaplanır.</para>
///
/// <para><b><see cref="ActorName"/> bilinçli olarak denormalizedir.</b> Kullanıcı kaydı
/// silinse bile iz okunur kalmalıdır; ayrıca okuma anında ad çözmek modüller arası sorgu
/// demektir ve yasaktır.</para>
///
/// <para><b><see cref="ErrorCode"/> saklanır, hata MESAJI saklanmaz.</b>
/// <c>Error.Code</c> makine okunurdur ve sabittir; mesaj PII taşıyabilir (öğrenci adı,
/// ilçe adı).</para>
/// </remarks>
public sealed record AuditEntry
{
    public Guid Id { get; init; }

    public DateTimeOffset OccurredAt { get; init; }

    public Guid ActorId { get; init; }

    /// <summary>Aktörün o andaki adı — denormalize; kayıt silinse de iz okunur kalır.</summary>
    public string ActorName { get; init; } = string.Empty;

    /// <summary>Komut tipinin kısa adı, ör. <c>MarkAttendance</c>. Makine anahtarıdır.</summary>
    public string CommandType { get; init; } = string.Empty;

    /// <summary>
    /// Komutun Türkçe arayüz etiketi. <b>Sunucudan gelir</b> — arayüz kendi eşleme tablosunu
    /// tutsaydı yeni bir komutta sessizce ham tip adı görünürdü. Sözlükte karşılığı olmayan
    /// komutta <see cref="CommandType"/> ile aynıdır.
    /// </summary>
    public string CommandLabel { get; init; } = string.Empty;

    /// <summary>Komutun ait olduğu modül, ör. <c>Attendance</c>.</summary>
    public string Module { get; init; } = string.Empty;

    /// <summary>Satırın yazıldığı kiracı. Kurum üstü işlerde <c>platform</c>.</summary>
    public string? TenantId { get; init; }

    public Guid? ActorInstitutionId { get; init; }

    public Guid? SubjectInstitutionId { get; init; }

    /// <summary>
    /// Konu kurumun ağaçtaki yolu. Okuma süzgeci bunu kullanır:
    /// <c>SubjectInstitutionPath.StartsWith(okuyucununYolu)</c> — A parçasındaki
    /// <c>InstitutionScopePolicy</c> ile aynı kural, yeni kapsam ekseni doğmaz.
    ///
    /// <para><c>null</c> = yol çözülemedi (geçiş ucu koşmamış ya da arama başarısız). Satır
    /// yine yazılır; yalnız yol önekiyle okuyan kullanıcıya görünmez.</para>
    /// </summary>
    public string? SubjectInstitutionPath { get; init; }

    /// <summary>
    /// Aktörün kurumu ile konu kurumu ayrıştığında <c>true</c>.
    /// <b>Hesaplanmış olarak saklanır</b> çünkü sonradan türetmek iki alanın o günkü
    /// değerini bilmeyi gerektirir — kurum ağacı değişince geçmiş yeniden yazılırdı.
    /// </summary>
    public bool CrossedTenantBoundary { get; init; }

    /// <summary>
    /// Sonucun <b>saklanan</b> hâli — <c>AuditOutcome.Name</c>.
    /// </summary>
    /// <remarks>
    /// <b>Neden düz string:</b> Marten LINQ'te <c>e.Outcome.Name</c> SQL'e
    /// <c>data->'outcome'->>'Name'</c> çevrilir; SmartEnum JSON'a düz string yazıldığı için
    /// bu yol HER ZAMAN NULL döner ve süzgeç hiçbir şey bulmaz — ne derleyici ne test görür.
    /// Aynı tuzak <c>Institution.NodeTypeName</c> yorumunda anlatıldı.
    /// </remarks>
    public string OutcomeName { get; init; } = AuditOutcome.Failed.Name;

    /// <summary><c>Rejected</c>'ta <c>Error.Code</c>, <c>Failed</c>'da istisna tipinin adı.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Komuttan konvansiyonla çıkarılan hedef kayıt kimlikleri (ör.
    /// <c>{"StudentId": …, "ContractId": …}</c>). Bilinen ad kümesinde olmayan komut
    /// <b>hedefsiz</b> kaydolur — satır yine oluşur.
    /// </summary>
    public Dictionary<string, Guid> TargetIds { get; init; } = [];

    public int DurationMs { get; init; }

    /// <summary>
    /// Sonuç tipi. <see cref="OutcomeName"/>'den hesaplanır ve <b>serialize edilmez</b> —
    /// tek stok alan olsun ki ikisi ayrışamasın.
    /// </summary>
    [JsonIgnore]
    public AuditOutcome Outcome => AuditOutcome.Resolve(OutcomeName);
}
