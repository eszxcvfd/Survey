using SurveyModel = Survey.Models.Survey;

namespace Survey.Repositories
{
    public interface ISurveyRepository
    {
        Task<SurveyModel?> GetByIdAsync(Guid surveyId);
        Task<SurveyModel?> GetByIdWithDetailsAsync(Guid surveyId);
        Task<List<SurveyModel>> GetByOwnerIdAsync(Guid ownerId);
        Task<List<SurveyModel>> GetAllAsync();
        Task<bool> IsOwnerAsync(Guid surveyId, Guid userId);
        Task AddAsync(SurveyModel survey);
        Task UpdateAsync(SurveyModel survey);
        Task DeleteAsync(Guid surveyId);
        Task<int> GetQuestionCountAsync(Guid surveyId);
        Task<int> GetResponseCountAsync(Guid surveyId);
    }
}