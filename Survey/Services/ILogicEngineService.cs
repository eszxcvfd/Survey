using Survey.Models;

namespace Survey.Services
{
    public interface ILogicEngineService
    {
        Task<Question?> GetNextQuestionAsync(Guid surveyId, Guid responseId, Guid? lastAnsweredQuestionId);
    }
}