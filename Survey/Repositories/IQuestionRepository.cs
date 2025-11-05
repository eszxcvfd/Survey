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
    }
}