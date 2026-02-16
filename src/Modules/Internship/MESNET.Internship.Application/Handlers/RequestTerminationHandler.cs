using MESNET.Common.Shared;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Shared.Events;

namespace MESNET.Internship.Application.Handlers;

public static class RequestTerminationHandler
{
    public static (Result, InternshipTerminationRequested?) Handle(RequestTermination command)
    {
        return (Result.Success(), new InternshipTerminationRequested(
            command.InternshipId,
            Guid.Empty,
            command.Reason,
            command.ReasonType,
            command.RequestedBy));
    }
}
