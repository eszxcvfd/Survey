using Survey.Models;

namespace Survey.Repositories
{
    public interface IQuestionRepository
    {
        Task<Question?> GetByIdAsync(Guid questionId);
        Task<Question?> GetByIdWithOptionsAsync(Guid questionId);
        Task<List<Question>> GetBySurveyIdAsync(Guid surveyId);
        Task<List<Question>> GetBySurveyIdWithOptionsAsync(Guid surveyId);
        Task AddAsync(Question question);
        Task UpdateAsync(Question question);
        Task DeleteAsync(Guid questionId);
        Task<int> GetMaxOrderBySurveyIdAsync(Guid surveyId);
        
        // NEW: Methods for Logic Engine
        Task<Question?> GetFirstQuestionAsync(Guid surveyId);
        Task<Question?> GetNextQuestionInOrderAsync(Guid surveyId, Guid currentQuestionId);
        Task<Question?> GetNextQuestionAfterAsync(Guid surveyId, Guid targetQuestionId);
    }
}