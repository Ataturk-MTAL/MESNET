namespace MESNET.Attendance.Application.Dtos;

/// <summary>
/// Fiilen uygulanan devamsızlık sınırları (#183). <b>Üç sayı, iki boyut:</b> eğitim türü ×
/// devamsızlık türü. Örgünün iki ayağı da bağlayıcıdır; MESEM'in yalnız toplam ayağı vardır —
/// yönetmelik işletme eğitimini izin hakkı toplamıyla karşılaştırır, devamsızlık türüne bakmaz.
/// </summary>
/// <param name="IsConfigured">
/// <c>false</c> ise kayıt hiç girilmemiş ve mevzuattan türetilmiş başlangıç değerleri
/// kullanılıyor demektir. Arayüz bunu görünür kılmalı — "sistem varsayılanı" ile "idarenin
/// girdiği değer" aynı görünmemeli.
/// </param>
public sealed record AbsenceLimitsDto(
    int FormalUnexcusedDayLimit,
    int FormalTotalDayLimit,
    int MesemTotalDayLimit,
    bool IsConfigured,
    DateTime? UpdatedAt);
