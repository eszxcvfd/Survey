using Survey.DTOs;

namespace Survey.Services
{
    public interface ISurveyDesignerService
    {
        Task<SurveyDesignerViewModel?> GetSurveyForDesignerAsync(Guid surveyId, Guid currentUserId);
        Task<ServiceResult<QuestionViewModel>> AddQuestionAsync(Guid surveyId, string questionType, Guid currentUserId);
        Task<ServiceResult<QuestionViewModel>> UpdateQuestionAsync(QuestionViewModel model, Guid currentUserId);
        Task<ServiceResult> DeleteQuestionAsync(Guid questionId, Guid currentUserId);
        Task<ServiceResult> ReorderQuestionsAsync(Guid surveyId, List<Guid> questionIds, Guid currentUserId);
    }
}