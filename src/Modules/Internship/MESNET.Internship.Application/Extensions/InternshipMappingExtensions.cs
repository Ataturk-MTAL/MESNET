using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.ValueObjects;

namespace MESNET.Internship.Application.Extensions;

public static class InternshipMappingExtensions
{
    public static InternshipSummaryDto ToDto(this InternshipSummary summary) => new(
        summary.Id,
        summary.StudentId,
        summary.BusinessId,
        summary.InstitutionId,
        summary.ContractId,
        summary.Phase.Name,
        summary.Phase.Slug,
        summary.StartedAt,
        summary.CompletedAt,
        summary.TotalAbsenceDays,
        summary.CompletedVisits,
        summary.ConfirmedPayments,
        summary.LastUpdated);

    public static TerminationApprovalChainDto ToDto(this TerminationApprovalChain chain) => new(
        chain.ParentApproved,
        chain.TeacherApproved,
        chain.DeputyApproved,
        chain.DirectorApproved,
        chain.BusinessRepApproved,
        chain.IsOverridden,
        chain.OverriddenBy,
        chain.OverriddenAt,
        chain.CompletedAt);
}
