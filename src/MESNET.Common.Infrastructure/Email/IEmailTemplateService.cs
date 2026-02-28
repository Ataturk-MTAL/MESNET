namespace MESNET.Common.Infrastructure.Email;

public interface IEmailTemplateService
{
    string RenderInvitation(string fullName, string targetRole, string registrationLink);
    byte[] GetLogoBytes();
}
