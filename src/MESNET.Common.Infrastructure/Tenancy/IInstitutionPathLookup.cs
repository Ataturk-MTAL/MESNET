namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Kurum kimliğinden ağaç yolunu (<c>/{ilId}/{ilçeId}/{okulId}/</c>) çözer.
/// </summary>
/// <remarks>
/// <para><b>Neden altyapıda:</b> denetim modülü <c>institution</c> şemasına sorgu ATAMAZ
/// (şema izolasyonu). Aynı arama <c>PermissionClaimsTransformation</c> içinde de yapılıyor —
/// o kopya <b>bilinçli olarak yerinde bırakıldı</b>: onun önbelleği KULLANICI başınadır ve
/// token geçersizleme yoluna bağlıdır; buradaki KURUM başınadır. İkisini tek önbellekte
/// birleştirmek, denetim yazmasını token geçersizleme yaşam döngüsüne bağlardı.</para>
///
/// <para><b>Boş sonuç hata değildir:</b> geçiş ucu (<c>POST /api/institutions/rebuild-hierarchy</c>)
/// o kurum için henüz koşmamış olabilir. <c>null</c> döner ve çağıran satırı yine yazar.</para>
/// </remarks>
public interface IInstitutionPathLookup
{
    Task<string?> GetPathAsync(Guid institutionId, CancellationToken cancellationToken = default);
}
