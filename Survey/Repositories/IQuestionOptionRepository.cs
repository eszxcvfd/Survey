using Survey.Models;

namespace Survey.Repositories
{
    public interface IQuestionOptionRepository
    {
        Task<QuestionOption?> GetByIdAsync(Guid optionId);
        Task<List<QuestionOption>> GetByQuestionIdAsync(Guid questionId);
        Task AddAsync(QuestionOption option);
        Task AddRangeAsync(List<QuestionOption> options);
        Task UpdateAsync(QuestionOption option);
        Task DeleteAsync(Guid optionId);
        Task DeleteRangeAsync(List<Guid> optionIds);
    }
}