namespace MESNET.Common.Infrastructure.Email;

public interface IEmailTemplateService
{
    string RenderInvitation(string fullName, string targetRole, string registrationLink);

    /// <summary>Kademeli devamsızlık bildirimi gövdesi (#247).</summary>
    string RenderAbsenceNotification(
        string recipientName, string studentName, string stepLabel, string legLabel,
        int days, IReadOnlyList<int> skippedSteps);
    byte[] GetLogoBytes();
}
