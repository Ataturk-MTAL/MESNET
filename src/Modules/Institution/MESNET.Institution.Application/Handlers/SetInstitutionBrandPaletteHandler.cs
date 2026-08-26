using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class SetInstitutionBrandPaletteHandler
{
    /// <summary>
    /// Paleti doğrular ve <b>anahtarı</b> saklar.
    ///
    /// <para>Doğrulama <c>TryFromName</c> ile yapılır ve saklanan değer daima
    /// <c>palette.Name</c>'dir — isteğin yazımı değil. Böylece kayıtta yalnız kanonik anahtar
    /// bulunur; "lacivert" ve "Lacivert" iki ayrı değer olarak veriye düşemez.</para>
    /// </summary>
    public static async Task<InstitutionBrandPaletteChanged> Handle(
        SetInstitutionBrandPalette command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId, cancellationToken);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

        // Küratörlü küme kapalıdır: hex gövdeden gelmez, anahtar burada palete çözülür.
        // Resolve() KULLANILMAZ — o, okuma tarafının varsayılana düşme davranışıdır; yazmada
        // tanınmayan anahtarı sessizce lacivert yapmak, kullanıcının seçimini yutardı.
        if (!InstitutionBrandPalette.TryFromName(command.PaletteName, ignoreCase: true, out var palette))
        {
            throw new DomainException(InstitutionErrors.UnknownBrandPalette(
                command.PaletteName,
                InstitutionBrandPalette.List.Select(p => p.Name)));
        }

        institution.BrandPaletteName = palette.Name;
        session.Store(institution);

        return new InstitutionBrandPaletteChanged(
            institution.Id, palette.Name, palette.Primary, palette.Secondary);
    }
}
