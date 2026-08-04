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
/// <para><b>Bu doğrulama sessiz hatayı gürültülü hâle getirir.</b> Adın nerede kaybolduğu
/// (Keycloak okuma, seeder gövdesi ya da model bağlama) bu iş kapsamında pinlenemedi; boş ad
/// artık 422 ile reddedileceği için bir sonraki yazma denemesi kaynağı kendiliğinden
/// gösterecek.</para>
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
