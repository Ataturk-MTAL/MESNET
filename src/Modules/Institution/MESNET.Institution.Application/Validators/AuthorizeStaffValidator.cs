using FluentValidation;
using MESNET.Institution.Application.Commands;

namespace MESNET.Institution.Application.Validators;

/// <summary>
/// Personel yetkilendirme doğrulaması (#190).
///
/// <para><b>Neden gerekti:</b> <c>StaffMember.FullName</c> <c>required</c> olarak tanımlı, ama
/// <c>required</c> yalnız <b>atanmış olmayı</b> zorunlu kılar — boş dizeyi kabul eder. Canlı
/// veride 205 personel kaydının <b>205'i</b> boş adla yazılmıştı: kurum sayfasındaki "Ad Soyad"
/// sütunu tümüyle boştu ve hata hiç fark edilmemişti, çünkü satırlar render olmaya devam
/// ediyordu.</para>
///
/// <para><b>Kök neden kapatıldı, bu doğrulama nöbetçi olarak kalır.</b> Adlar Keycloak'ta
/// siliniyordu: öznitelik yazan PUT gövdesi yalnız <c>attributes</c> taşıyordu, Keycloak da
/// gövdede geçmeyen <c>firstName</c>/<c>lastName</c>/<c>email</c> alanlarını silip yine
/// <b>204</b> dönüyordu. Seeder her koşuda önce adları siliyor, sonra o boş adı okuyup
/// personel kaydı açıyordu. Gövde artık tam temsildir.</para>
///
/// <para><b>Sıralama kritiktir:</b> bu doğrulama, kaynak düzelmeden açılırsa seeder her boş
/// veritabanında 422 alır — CI'da bir kez yaşandı ve bu yüzden #193'te ertelendi. Kilitleyen
/// testler: <c>MESNET.Seeder.UnitTests.KeycloakProfilePreservationTests</c> ve
/// <c>MESNET.Security.UnitTests.KeycloakProfilePreservationTests</c>.</para>
/// </summary>
public class AuthorizeStaffValidator : AbstractValidator<AuthorizeStaff>
{
    public AuthorizeStaffValidator()
    {
        RuleFor(x => x.KeycloakId)
            .NotEmpty()
            .WithMessage("Personelin kullanıcı kimliği belirtilmelidir.");

        // Boş ad, listede kimliği çözülemeyen bir satır üretir; kayıt teknik olarak geçerli
        // görünür ama hiçbir işe yaramaz.
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Personelin adı soyadı belirtilmelidir.");
    }
}
