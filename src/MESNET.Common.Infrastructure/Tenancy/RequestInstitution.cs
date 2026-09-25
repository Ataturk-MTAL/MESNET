using MESNET.Common.Shared.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// İsteğin çalıştığı okulu, isteğin <b>kiracısından</b> türetir.
///
/// <para><b>Neden <c>institution_id</c> claim'i okunmaz:</b> claim kullanıcının EV kurumudur;
/// aktif bağlam (<c>active_institution_id</c>) ayrı claim'dedir ve kiracıyı o belirler
/// (<see cref="TenantResolution.Resolve"/>). Okula geçen il yetkilisinde ikisi ayrışır: satırlar
/// row-level security ile aktif okula süzülür, sorgu ise ev kurumuna filtrelenir — sonuç hata
/// değil <b>boş liste</b> olur. Kurumu kiracıdan türetmek, filtre ile satır süzgecinin aynı
/// okulu göstermesini yapısal olarak garanti eder (#309 ile aynı ilke).</para>
///
/// <para>Okul olmayan kiracıda (platform, kiracısız) <see cref="Guid.Empty"/> döner; handler'lar
/// bunu zaten "kapsam yok" olarak reddeder.</para>
/// </summary>
public static class RequestInstitution
{
    public static Guid Of(HttpContext http) =>
        TenantResolution.InstitutionOf(http.RequestServices.GetRequiredService<IMessageBus>().TenantId)
        ?? Guid.Empty;
}
