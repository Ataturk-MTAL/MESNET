using FluentValidation;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Errors;

namespace MESNET.Security.Application.Validators;

/// <summary>
/// Rol adı doğrulaması — <b>sunucu savunması</b> (#129).
///
/// <para>Arayüzün rol listesini API'den alması yeterli değildir: istek doğrudan da atılabilir,
/// eski bir istemci önbellekte kalabilir, seeder yanlış ad yazabilir. Tanınmayan bir rol adı
/// Keycloak'ta çözülemez ve kullanıcı <b>sıfır realm rolüyle</b> açılırdı — hiçbir izin almadan,
/// hiçbir hata görmeden. Bu yüzden tanınmayan ad <b>sınırda</b> reddedilir.</para>
///
/// <para>Tek doğruluk kaynağı <see cref="MesnetRoles.All"/>; hata metni ve kodu
/// <see cref="SecurityErrors.InvalidRole"/> içindedir.</para>
/// </summary>
public static class RoleValidationRules
{
    private static readonly string InvalidRoleCode = SecurityErrors.InvalidRole(string.Empty).Code;

    /// <summary>Tek rol adı <see cref="MesnetRoles.All"/> üyesi olmalıdır.</summary>
    public static IRuleBuilderOptions<T, string> MustBeKnownRole<T>(
        this IRuleBuilder<T, string> rule) =>
        rule.Must(MesnetRoles.IsValid)
            .WithErrorCode(InvalidRoleCode)
            .WithMessage((_, role) => SecurityErrors.InvalidRole(role ?? string.Empty).Description);

    /// <summary>Listedeki her rol adı <see cref="MesnetRoles.All"/> üyesi olmalıdır.</summary>
    public static IRuleBuilderOptions<T, List<string>> MustBeKnownRoles<T>(
        this IRuleBuilder<T, List<string>> rule) =>
        rule.Must(roles => roles is null || roles.All(MesnetRoles.IsValid))
            .WithErrorCode(InvalidRoleCode)
            .WithMessage((_, roles) => SecurityErrors.InvalidRole(
                string.Join(", ", (roles ?? []).Where(r => !MesnetRoles.IsValid(r)))).Description);
}
