namespace MESNET.Institution.Application.Security;

/// <summary>
/// Hedef kurumu <b>istekten</b> alan command/query'leri işaretler (ADR-0003 adım 6).
///
/// <para>Bu marker'ı taşıyan her mesaj, handler çalışmadan önce
/// <see cref="InstitutionScopeGuardMiddleware"/>'den geçer: aktörün kurum kapsamı hedefle
/// eşleşmiyorsa istek <see cref="MESNET.Common.Shared.DomainException"/> ile reddedilir.</para>
///
/// <para><b>Neden marker + middleware, handler içinde tek tek kontrol değil:</b> kurum kimliğini
/// istekten alan uç sayısı artıyor ve unutulan tek bir kontrol, başka okulun kaydını açar.
/// Kontrolü mesaj tipine bağlamak, "yeni uç eklendi ama kapsam kontrolü unutuldu" durumunu
/// mümkün olduğunca daraltır; kalanını <c>InstitutionScopeDriftTests</c> kapatır.</para>
/// </summary>
public interface IInstitutionScoped
{
    /// <summary>İstekte geçen hedef kurum kimliği.</summary>
    Guid InstitutionId { get; }
}
