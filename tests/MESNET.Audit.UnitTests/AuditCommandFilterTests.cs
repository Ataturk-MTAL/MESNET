using MESNET.Audit.Application.Auditing;
using Shouldly;
using Xunit;

namespace MESNET.Audit.UnitTests;

/// <summary>
/// Süzgeç bir AD ALANI konvansiyonudur; depoda zaten klasör yapısıyla zorlanıyor
/// (<c>Commands/</c> ve <c>Queries/</c> ayrı) ve <c>InstitutionScopeDriftTests</c> de ona
/// dayanıyor — yeni bir kural icat edilmiyor, var olan kural kullanılıyor.
/// </summary>
public class AuditCommandFilterTests
{
    [Fact]
    public void Commands_ad_alanindaki_tip_denetlenir()
    {
        AuditCommandFilter
            .ShouldAudit(typeof(MESNET.AuditFixtures.Sample.Application.Commands.MarkAttendanceSample))
            .ShouldBeTrue();
    }

    [Fact]
    public void Queries_ad_alanindaki_tip_denetlenmez()
    {
        // Okuma iz üretmez; aksi hâlde hacim listeleme trafiğiyle dolar.
        AuditCommandFilter
            .ShouldAudit(typeof(MESNET.AuditFixtures.Sample.Application.Queries.GetAttendanceSample))
            .ShouldBeFalse();
    }

    [Fact]
    public void Consumers_ad_alanindaki_tip_denetlenmez()
    {
        // Tüketiciler kullanıcı eylemi değil OLAY TEPKİSİDİR. Kullanıcı eylemi zaten onu
        // tetikleyen komutta kaydedilmiştir; ikinci kez yazmak zinciri çift gösterirdi.
        AuditCommandFilter
            .ShouldAudit(typeof(MESNET.AuditFixtures.Sample.Application.Consumers.AttendanceMarkedSample))
            .ShouldBeFalse();
    }

    [Fact]
    public void Commands_ad_alanindaki_Get_ile_baslayan_tip_denetlenmez()
    {
        // Depoda Commands/ klasörüne YANLIŞ yerleştirilmiş sorgular var (GetUserAccounts,
        // GetDocumentById, GetInvitations …). Ad alanı onları komut sanardı ve liste
        // trafiğinin tamamı ize düşerdi. Bu ikinci kural o yanlış yerleşimin bedelidir.
        AuditCommandFilter
            .ShouldAudit(typeof(MESNET.AuditFixtures.Sample.Application.Commands.GetUserAccountsSample))
            .ShouldBeFalse();
    }

    [Fact]
    public void Ad_alani_olmayan_tip_denetlenmez()
    {
        AuditCommandFilter.ShouldAudit(typeof(int)).ShouldBeFalse();
    }
}
