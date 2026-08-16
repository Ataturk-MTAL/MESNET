namespace MESNET.Attendance.Application.Dtos;

/// <param name="IsConfigured">
/// <c>false</c> ise kayıt hiç girilmemiş ve mevzuattan türetilmiş başlangıç değerleri
/// kullanılıyor demektir (#183). Arayüz bunu görünür kılmalı — "sistem varsayılanı" ile
/// "idarenin girdiği değer" aynı görünmemeli.
/// </param>
public sealed record AbsenceLimitsDto(
    int FormalUnexcusedDayLimit,
    int MesemUnexcusedDayLimit,
    bool IsConfigured,
    DateTime? UpdatedAt);
