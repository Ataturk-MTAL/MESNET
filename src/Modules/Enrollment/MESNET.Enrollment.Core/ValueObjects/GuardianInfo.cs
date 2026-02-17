namespace MESNET.Enrollment.Core.ValueObjects;

public sealed record GuardianInfo(
    string FullName,
    string TcKimlikNo,
    string Phone,
    string Address,
    string Relationship);
