using Survey.Models;

namespace Survey.Repositories
{
    public interface ISurveyCollaboratorRepository
    {
        Task<SurveyCollaborator?> GetAsync(Guid surveyId, Guid userId);
        Task<List<SurveyCollaborator>> GetBySurveyIdAsync(Guid surveyId);
        Task<List<SurveyCollaborator>> GetByUserIdAsync(Guid userId);
        Task<bool> ExistsAsync(Guid surveyId, Guid userId);
        Task AddAsync(SurveyCollaborator collaborator);
        Task UpdateAsync(SurveyCollaborator collaborator);
        Task DeleteAsync(Guid surveyId, Guid userId);
        Task<int> GetCollaboratorCountAsync(Guid surveyId);
        Task<string?> GetRoleAsync(Guid surveyId, Guid userId); // ✅ Thêm method này
    }
}