namespace MESNET.Enrollment.Application.Commands;

public sealed record DeregisterStudent(Guid StudentId, string Reason);
