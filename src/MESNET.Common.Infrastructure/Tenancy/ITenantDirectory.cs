namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Sistemdeki kiracıların listesi (#149).
///
/// <para><b>Neden var:</b> arka plan işleri bir isteğe bağlı değildir, dolayısıyla kiracıyı
/// istekten devralamazlar. Kiracılık açıldıktan sonra kiracısız bir Marten session'ı
/// <c>DefaultTenantUsageDisabledException</c> fırlatır — yani aylık maaş dönemi açma ve aylık
/// rapor üretimi gibi zamanlanmış işler, hangi kiracı adına çalışacaklarını <b>söylemek
/// zorundadır</b>.</para>
///
/// <para><b>Neden arayüz, doğrudan sorgu değil:</b> kiracı = okul (ADR-0003) ve okul kaydı
/// Institution modülünündür. Altyapının <c>institution</c> şemasına doğrudan SQL atması şema
/// izolasyonunu kırardı. Uygulaması modülde, sözleşmesi burada —
/// <c>IUserPermissionProvider</c> ile aynı desen.</para>
///
/// <para><b>Boş liste hata değildir:</b> henüz hiç okul kaydı yoksa zamanlanmış iş yapacak bir
/// şey bulamaz. Sessizce varsayılan kiracıya düşmek yerine hiç çalışmamak doğrudur.</para>
/// </summary>
public interface ITenantDirectory
{
    Task<IReadOnlyList<string>> GetActiveTenantsAsync(CancellationToken cancellationToken = default);
}
