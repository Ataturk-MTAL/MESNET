namespace MESNET.Business.Application.Commands;

/// <summary>
/// Tüm işletmeler için <c>BusinessUpdated</c> olayını yeniden yayınlar — diğer modüllerin
/// denormalize işletme read-model'lerini tazeler.
/// </summary>
/// <remarks>
/// Olay-beslemeli read-model'e yeni alan eklendiğinde mevcut document'lar bayat kalır; idempotent
/// seeder kaynağı yeniden yaratmadığı için olay tekrar yayınlanmaz. Örnek: <c>PersonnelCount</c>
/// Payment'ın <c>BusinessPaymentProfile</c>'ına eklendiğinde (#64) mevcut işletmeler için sıfır
/// kalıyordu — o durumda tüm işletmeler "küçük" sayılıp taban ücret oranı %30 yerine %15
/// uygulanırdı (#77).
///
/// Tüm consumer'lar idempotent upsert (<c>session.Store</c>) yaptığından tekrar çalıştırmak güvenlidir.
/// </remarks>
public sealed record ResyncBusinessProjections;

public sealed record ResyncBusinessProjectionsResult(int BusinessCount);
