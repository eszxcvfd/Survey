using Survey.DTOs;

namespace Survey.Services
{
    public interface ISurveyTakerService
    {
        Task<ServiceResult<Guid>> CreateNewResponseAsync(Guid surveyId, Guid? channelId, string? respondentIP);
        Task<ServiceResult> SaveAnswerAsync(SubmitAnswerViewModel model);
        Task<ServiceResult> CompleteResponseAsync(Guid responseId);
        Task<bool> ValidateResponseAccessAsync(Guid responseId, string sessionToken);
    }
}