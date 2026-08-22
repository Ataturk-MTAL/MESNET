using MESNET.Payment.Application.Dtos;
using MESNET.Payment.Core.Entities;

namespace MESNET.Payment.Application.Extensions;

public static class PaymentMappingExtensions
{
    public static PaymentSummaryDto ToDto(this PaymentSummary summary) => new(
        summary.Id,
        summary.StudentId,
        summary.BusinessId,
        summary.InstitutionId,
        summary.Month,
        summary.BaseWage,
        summary.DeductionAmount,
        summary.NetAmount,
        summary.GovernmentContribution,
        summary.EmployerPayment,
        summary.EmployedDays,
        summary.Phase.Name,
        summary.Phase.Slug,
        summary.ReceiptId,
        summary.ReceiptObjectPath,
        summary.UploadedByStudent,
        summary.ReceiptDueDate,
        summary.StudentConfirmedAt,
        summary.TeacherApprovedAt,
        summary.DeputyApprovedAt,
        summary.LastUpdated,
        summary.StudentName,
        summary.StudentNumber,
        summary.BranchCode);
}
