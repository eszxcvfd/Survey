using Survey.Models;

namespace Survey.Repositories
{
    public interface IResponseAnswerRepository
    {
        Task<ResponseAnswer?> GetAnswerAsync(Guid responseId, Guid questionId);
        Task<List<ResponseAnswerOption>> GetAnswerOptionsAsync(Guid responseId, Guid questionId);
        Task AddOrUpdateAnswerAsync(ResponseAnswer answer);
        Task AddAnswerOptionAsync(ResponseAnswerOption answerOption);
        Task DeleteAnswerOptionsAsync(Guid responseId, Guid questionId);
    }
}