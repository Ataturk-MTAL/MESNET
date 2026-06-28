using MESNET.Internship.Application.Commands;
using MESNET.Internship.Shared.Events;

namespace MESNET.Internship.Application.Handlers;

public static class RequestTerminationHandler
{
    public static InternshipTerminationRequested Handle(RequestTermination command)
    {
        return new InternshipTerminationRequested(
            command.InternshipId,
            command.Reason,
            command.ReasonType,
            command.RequestedBy);
    }
}
