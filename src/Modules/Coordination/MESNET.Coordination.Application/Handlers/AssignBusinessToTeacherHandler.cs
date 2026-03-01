using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class AssignBusinessToTeacherHandler
{
    public static async Task Handle(
        AssignBusinessToTeacher command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var view = await session.LoadAsync<BusinessCoordinationView>(
            command.BusinessId, cancellationToken);

        if (view is null)
            throw new DomainException(CoordinationErrors.BusinessNotFound(command.BusinessId));

        // Kısıt 1: TakdirEdilenSaat ≤ VerilebilirSaat (işletme bazında)
        if (command.AssignedHours > view.MaxCoordinationHours)
        {
            throw new DomainException(
                CoordinationErrors.AssignedHoursExceedMax(command.AssignedHours, view.MaxCoordinationHours));
        }

        // Kısıt 2: Toplam dağıtılan saat ≤ ToplamVerilebilirSaat (alan bazında)
        var allViews = await session.Query<BusinessCoordinationView>()
            .Where(v => v.InstitutionId == command.InstitutionId && v.BranchCode == view.BranchCode)
            .ToListAsync(cancellationToken);

        var totalAvailable = allViews.Sum(v => v.MaxCoordinationHours);
        var totalAssigned = allViews
            .Where(v => v.Id != command.BusinessId)
            .Sum(v => v.AssignedHours) + command.AssignedHours;

        if (totalAssigned > totalAvailable)
        {
            throw new DomainException(
                CoordinationErrors.TotalAssignedHoursExceedAvailable(totalAssigned, totalAvailable));
        }

        // Atama yap
        view.AssignedTeacherId = command.TeacherId;
        view.AssignedTeacherName = command.TeacherName;
        view.AssignedHours = command.AssignedHours;
        view.AssignedDay = command.AssignedDay;

        session.Store(view);
    }
}
