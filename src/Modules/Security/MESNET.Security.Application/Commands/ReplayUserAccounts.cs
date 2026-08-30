namespace MESNET.Security.Application.Commands;

/// <summary>
/// Bütün kullanıcı hesaplarını <c>UserCreated</c> olarak yeniden yayınlar (D2) — diğer
/// modüllerin yerel görünümlerini geriye dönük doldurmak için. İdempotenttir: tüketiciler
/// satırı mutlak olarak yazar.
///
/// <para><b>Neden yeniden yayın, doğrudan yazma değil:</b> bir modülün başka modülün
/// belgesine yazması yasaktır. Emsal: <c>POST /api/institutions/staff/resync-branch-codes</c>.</para>
/// </summary>
public sealed record ReplayUserAccounts;
