namespace MESNET.Contract.Application.Commands;

public sealed record CreateContract(
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId,
    DateTime StartDate,
    /// <summary>İşletmenin sözleşmede taahhüt ettiği aylık ücret. null ise yasal taban geçerli (#84).</summary>
    decimal? AgreedMonthlyWage = null);
