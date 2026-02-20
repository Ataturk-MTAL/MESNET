using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class SkillExamHandler
{
    public static async Task<Guid> Handle(
        CreateSkillExam command,
        IDocumentSession session,
        CancellationToken ct)
    {
        if (!AcademicSemester.TryFromName(command.Semester, true, out var semester))
            throw new DomainException(CoordinationErrors.InvalidSemester(command.Semester));

        if (!ExamResult.TryFromName(command.Result, true, out var examResult))
            throw new DomainException(CoordinationErrors.InvalidExamResult(command.Result));

        var exam = new SkillExam
        {
            Id = Guid.NewGuid(),
            StudentId = command.StudentId,
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            AcademicYear = command.AcademicYear,
            Semester = semester,
            ExamDate = command.ExamDate,
            Score = command.Score,
            Criteria = command.Criteria,
            CommitteeMembers = command.CommitteeMembers,
            Result = examResult
        };

        session.Store(exam);
        await session.SaveChangesAsync(ct);

        return exam.Id;
    }

    public static async Task Handle(
        UpdateSkillExam command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var exam = await session.LoadAsync<SkillExam>(command.ExamId, ct);
        if (exam is null)
            throw new DomainException(CoordinationErrors.ExamNotFound(command.ExamId));

        if (!ExamResult.TryFromName(command.Result, true, out var examResult))
            throw new DomainException(CoordinationErrors.InvalidExamResult(command.Result));

        exam.ExamDate = command.ExamDate;
        exam.Score = command.Score;
        exam.Criteria = command.Criteria;
        exam.CommitteeMembers = command.CommitteeMembers;
        exam.Result = examResult;

        session.Store(exam);
        await session.SaveChangesAsync(ct);
    }
}
