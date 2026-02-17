using MESNET.Common.Shared.Security;

namespace MESNET.Reporting.Application.Commands;

/// <summary>
/// Dokümanı "Yazdırıldı" olarak işaretle
/// </summary>
public sealed record MarkDocumentAsPrinted(Guid DocumentId, UserContext User);

/// <summary>
/// Dokümanı "İmzalanıp Teslim Edildi" olarak işaretle
/// </summary>
public sealed record MarkDocumentAsSignedAndReturned(Guid DocumentId, UserContext User);

/// <summary>
/// Dokümanı "Arşivlendi" olarak işaretle
/// </summary>
public sealed record MarkDocumentAsArchived(Guid DocumentId, UserContext User);

/// <summary>
/// Dokümanı sil (Marten + MinIO)
/// </summary>
public sealed record DeleteDocument(Guid DocumentId, UserContext User);

/// <summary>
/// Seçili dokümanları toplu sil (Marten + MinIO).
/// DİKKAT: Geri alınamaz işlemdir — hem veritabanı kaydı hem MinIO'daki PDF snapshot'ı kalıcı olarak silinir.
/// Sadece kullanıcının açıkça seçtiği ID'ler silinir, "tümünü sil" gibi toplu operasyon yapılmaz.
/// Frontend'de onay dialogu zorunludur.
/// </summary>
public sealed record DeleteDocumentsBatch(List<Guid> DocumentIds, UserContext User);
