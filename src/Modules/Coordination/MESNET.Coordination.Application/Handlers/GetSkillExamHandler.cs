using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class GetSkillExamHandler
{
    public static async Task<SkillExam> Handle(
        GetSkillExam query, IQuerySession session)
    {
        var exam = await session.LoadAsync<SkillExam>(query.ExamId);
        if (exam is null)
            throw new DomainException(new Error("SkillExam.NotFound", $"Beceri sınavı bulunamadı: {query.ExamId}"));

        return exam;
    }
}
