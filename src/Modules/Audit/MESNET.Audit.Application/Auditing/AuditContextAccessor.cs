namespace MESNET.Audit.Application.Auditing;

/// <summary>
/// <c>Before</c>'da kurulan bağlamı <c>OnExceptionAsync</c>'e taşıyan <b>scoped</b> köprü.
/// </summary>
/// <remarks>
/// <para><b>Neden var:</b> Wolverine'in ürettiği kodda <c>catch</c> bloğu, <c>try</c>'dan
/// önce üretilen değişkenleri görmez — <c>OnException(Exception, AuditContext)</c> yazmak
/// derlemeyi <c>CS0103</c> ile kırar (ölçüldü). Bağlamı taşımanın tek yolu DI'dır.</para>
///
/// <para><b>Neden <c>AsyncLocal</c> değil scoped servis:</b> istek kapsamı Wolverine
/// <c>InvokeAsync</c> çağrısının tamamını sarar ve DI zaten o kapsamı yönetir;
/// <c>AsyncLocal</c> eklemek ikinci bir yaşam döngüsü icat etmek olurdu.</para>
///
/// <para><b>Tek komut varsayımı:</b> bir kapsamda iç içe komut çalışırsa (handler içinden
/// <c>InvokeAsync</c>) iç komut dıştakini EZER ve dış komutun istisna satırı iç komutun
/// bağlamıyla yazılır. Bu depoda handler'dan handler'a <c>InvokeAsync</c> YASAKTIR
/// (CLAUDE.md); varsayım oraya dayanır.</para>
/// </remarks>
public sealed class AuditContextAccessor
{
    public AuditContext? Current { get; private set; }

    public void Set(AuditContext context) => Current = context;
}
