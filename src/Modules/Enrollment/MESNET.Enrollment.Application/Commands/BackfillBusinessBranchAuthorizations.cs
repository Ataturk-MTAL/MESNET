namespace MESNET.Enrollment.Application.Commands;

/// <summary>
/// Geçiş dolgusu (#119): mevcut fiilî yerleştirmelerden işletmelerin alan yetkilerini üretir.
/// </summary>
/// <remarks>
/// Alan yetkisi kuralı devreye girdiğinde mevcut işletmelerin yetki listesi boştur ve boş liste
/// KAPALI sayılır — dolgusuz bırakılırsa tüm mevcut yerleştirmeler kural dışı görünür ve yeni
/// yerleştirme hiçbir işletmeye yapılamaz.
///
/// Dolgu Enrollment'ta hesaplanır: kaynak veri (<c>InternshipPlacement.BranchCode</c>) bu modülün
/// şemasındadır ve Business başka modülün şemasını sorgulayamaz. İşletme başına farklı alan
/// kodları <c>BusinessBranchUsageObserved</c> olayıyla Business'a taşınır; Business yalnız EKSİK
/// yetkileri ekler, hiçbir yetkiyi iptal etmez.
///
/// Yalnız sonlanmamış (IsFinal olmayan) yerleştirmeler dikkate alınır — tamamlanmış/fesihli
/// kayıtlardan yetki üretmek yanlış olurdu.
///
/// Tüketici tarafı idempotent (var olan aktif yetki tekrar eklenmez); tekrar çalıştırmak güvenlidir.
/// </remarks>
public sealed record BackfillBusinessBranchAuthorizations;

public sealed record BackfillBusinessBranchAuthorizationsResult(int BusinessCount, int BranchCount);
