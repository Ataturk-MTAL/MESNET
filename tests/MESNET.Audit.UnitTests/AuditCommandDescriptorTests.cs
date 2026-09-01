using MESNET.Audit.Core.Services;
using Shouldly;
using Xunit;

namespace MESNET.Audit.UnitTests
{
    public class AuditCommandDescriptorTests
    {
        [Fact]
        public void Modul_adini_ad_alanindan_okur()
        {
            // MESNET.<Modül>.Application.Commands.<Komut> → "AuditFixtures"
            var (commandType, module) = AuditCommandDescriptor.Describe(
                typeof(MESNET.AuditFixtures.Sample.Application.Commands.MarkAttendanceSample));

            commandType.ShouldBe("MarkAttendanceSample");
            module.ShouldBe("AuditFixtures");
        }

        [Fact]
        public void Beklenmeyen_ad_alaninda_modul_bos_kalir_tip_adi_yine_yazilir()
        {
            // Satırın kendisi kaybolmamalı: modül bilinmese de "kim, ne" durur.
            var (commandType, module) = AuditCommandDescriptor.Describe(typeof(NoNamespaceCommand));

            commandType.ShouldBe("NoNamespaceCommand");
            module.ShouldBeEmpty();
        }
    }
}

/// <summary>
/// Kasıtlı olarak HİÇBİR ad alanında (global ad alanı) tanımlıdır — "beklenmeyen ad alanı"
/// senaryosunu gerçekten temsil etmesi için gerekli.
/// </summary>
/// <remarks>
/// Bu tip <c>AuditCommandDescriptorTests</c> sınıfının İÇİNE iç içe tanımlanırsaydı
/// <c>MESNET.Audit.UnitTests</c> ad alanını miras alırdı; <see cref="AuditCommandDescriptor"/>
/// bunu "MESNET." ile başlayan geçerli bir ad alanı sanıp modülü YANLIŞLIKLA "Audit" olarak
/// çözerdi — test projesinin kendi adı modül adlandırma konvansiyonuyla çakıştığı için.
/// Ölçüldü: brief'teki iç içe tanım biçimiyle bu test KIRMIZI kalıyordu (module "Audit" döndü,
/// boş değil). Çözüm: tipi dosya kapsamı dışına, gerçek ad alanı olmayan bir konuma taşımak.
/// </remarks>
public sealed record NoNamespaceCommand(Guid StudentId);
