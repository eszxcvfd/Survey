using Survey.DTOs;
using SurveyModel = Survey.Models.Survey;

namespace Survey.Services
{
    public interface ISurveyService
    {
        Task<ServiceResult<SurveyModel>> CreateSurveyAsync(CreateSurveyDto model, Guid ownerId);
        Task<MySurveysViewModel> GetMySurveysAsync(Guid currentUserId, string filter = "all");
        Task<SurveyModel?> GetSurveyByIdAsync(Guid surveyId);
        Task<ServiceResult> UpdateSurveyAsync(Guid surveyId, CreateSurveyDto model, Guid currentUserId);
        Task<ServiceResult> DeleteSurveyAsync(Guid surveyId, Guid currentUserId);
        Task<bool> HasAccessAsync(Guid surveyId, Guid userId);
        
        // New methods for Survey Settings
        Task<SurveySettingsViewModel?> GetSurveySettingsAsync(Guid surveyId, Guid currentUserId);
        Task<ServiceResult> UpdateSurveySettingsAsync(SurveySettingsViewModel model, Guid currentUserId);
    }
}