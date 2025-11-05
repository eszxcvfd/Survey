using Survey.Models;

namespace Survey.Repositories
{
    public interface ISurveyResponseRepository
    {
        Task<SurveyResponse?> GetByIdAsync(Guid responseId);
        Task<SurveyResponse?> GetByIdWithAnswersAsync(Guid responseId);
        Task<List<SurveyResponse>> GetBySurveyIdAsync(Guid surveyId);
        Task AddAsync(SurveyResponse response);
        Task UpdateAsync(SurveyResponse response);
        Task DeleteAsync(Guid responseId);
    }
}