namespace MESNET.Business.Core.ValueObjects;

public sealed record MasterInstructorInfo(
    string FullName,
    string TcKimlikNo,
    string CertificateNumber,
    string CertificateType);
