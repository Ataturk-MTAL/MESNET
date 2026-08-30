namespace MESNET.Security.Application.Commands;

/// <summary>
/// Bütün kullanıcı hesaplarını <c>UserAccountReplayed</c> olarak yeniden yayınlar (D2, I-2) —
/// diğer modüllerin yerel görünümlerini geriye dönük doldurmak için. İdempotenttir:
/// tüketiciler satırı mutlak olarak yazar.
///
/// <para><b>Neden <c>UserCreated</c> DEĞİL:</b> o olayı dinleyen diğer modüllerin tüketicileri
/// (Business, Enrollment, Institution personel kaydı) onu "yeni kayıt" sanır; boş yayın
/// silinmiş kayıtları eksik alanlarla diriltir. Bkz. <c>UserAccountReplayed</c> XML doc.</para>
///
/// <para><b>Neden yeniden yayın, doğrudan yazma değil:</b> bir modülün başka modülün
/// belgesine yazması yasaktır. Emsal: <c>POST /api/institutions/staff/resync-branch-codes</c>.</para>
/// </summary>
public sealed record ReplayUserAccounts;
