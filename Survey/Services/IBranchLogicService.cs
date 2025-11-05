using Survey.DTOs;

namespace Survey.Services
{
    public interface IBranchLogicService
    {
        Task<ManageLogicViewModel?> GetLogicForSurveyAsync(Guid surveyId, Guid currentUserId);
        Task<ServiceResult> AddRuleAsync(AddRuleViewModel model, Guid currentUserId);
        Task<ServiceResult> DeleteRuleAsync(Guid logicId, Guid currentUserId);
        Task<ServiceResult> UpdateRulePriorityAsync(Guid logicId, int newPriority, Guid currentUserId);
    }
}