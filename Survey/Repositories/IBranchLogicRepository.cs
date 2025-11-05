using Survey.Models;

namespace Survey.Repositories
{
    public interface IBranchLogicRepository
    {
        Task<BranchLogic?> GetByIdAsync(Guid logicId);
        Task<List<BranchLogic>> GetBySurveyIdAsync(Guid surveyId);
        Task<List<BranchLogic>> GetBySourceQuestionIdAsync(Guid sourceQuestionId);
        Task AddAsync(BranchLogic rule);
        Task UpdateAsync(BranchLogic rule);
        Task DeleteAsync(Guid logicId);
        Task<int> GetMaxPriorityOrderBySurveyIdAsync(Guid surveyId);
    }
}