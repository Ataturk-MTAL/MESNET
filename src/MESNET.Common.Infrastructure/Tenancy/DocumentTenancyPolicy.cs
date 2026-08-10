using JasperFx.MultiTenancy;
using Marten;
using Marten.Schema;
using MESNET.Common.Shared.Tenancy;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Kiracı damgasını <b>tek bir yerden</b>, <see cref="DocumentTenancyMap"/> sınıflandırmasına
/// göre uygular (#149).
///
/// <para><b>Neden politika, modül modül <c>MultiTenanted()</c> değil:</b> damga kararı on ayrı
/// <c>IConfigureMarten</c> dosyasına dağılsaydı sınıflandırma ile yapılandırma <b>ayrı iki
/// gerçek</b> olurdu ve zamanla kayardı. Burada tek kaynak var: haritada <c>Tenant</c> yazan
/// damgalanır, yazmayan damgalanmaz.</para>
///
/// <para><b><c>AllDocumentsAreMultiTenanted()</c> KULLANILAMAZ.</b> Kullanılsaydı ulusal
/// alan/dal kataloğu, ulusal ücret parametreleri, kimlik katmanı ve <b>paylaşımlı işletme
/// kataloğu</b> da damga alırdı. Damgayı geri almak tablo yeniden inşası + veri göçü demektir —
/// tek yönlü kapının yanlış tarafı.</para>
///
/// <para><b>Bilinmeyen tip damgalanmaz.</b> Marten'ın ve Wolverine'in kendi belge tipleri de bu
/// politikadan geçer; onlar haritada yoktur ve damgalanmamalıdır. MESNET tiplerinin haritada
/// bulunması iki katmanda kilitli: <c>DocumentTenancyDriftTests</c> (kaynak taraması) ve
/// <c>DocumentTenancyVerificationHostedService</c> (açılışta, Marten'ın gerçekten tanıdığı
/// tipler üzerinden).</para>
/// </summary>
public sealed class DocumentTenancyPolicy : IDocumentPolicy
{
    public void Apply(DocumentMapping mapping)
    {
        if (!DocumentTenancyMap.All.TryGetValue(mapping.DocumentType.Name, out var tenancy))
            return;

        if (tenancy == DocumentTenancy.Tenant)
            mapping.TenancyStyle = TenancyStyle.Conjoined;
    }
}
