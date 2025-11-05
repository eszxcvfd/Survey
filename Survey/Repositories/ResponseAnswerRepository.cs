using Microsoft.EntityFrameworkCore;
using Survey.Models;

namespace Survey.Repositories
{
    public class ResponseAnswerRepository : IResponseAnswerRepository
    {
        private readonly SurveyDbContext _context;

        public ResponseAnswerRepository(SurveyDbContext context)
        {
            _context = context;
        }

        public async Task<ResponseAnswer?> GetAnswerAsync(Guid responseId, Guid questionId)
        {
            return await _context.ResponseAnswers
                .Include(ra => ra.Question)
                .FirstOrDefaultAsync(ra => ra.ResponseId == responseId && ra.QuestionId == questionId);
        }

        public async Task<List<ResponseAnswerOption>> GetAnswerOptionsAsync(Guid responseId, Guid questionId)
        {
            return await _context.Set<ResponseAnswerOption>()
                .Include(rao => rao.Option)
                .Where(rao => rao.ResponseId == responseId && rao.QuestionId == questionId)
                .ToListAsync();
        }

        public async Task AddOrUpdateAnswerAsync(ResponseAnswer answer)
        {
            var existing = await GetAnswerAsync(answer.ResponseId, answer.QuestionId);
            if (existing != null)
            {
                existing.AnswerText = answer.AnswerText;
                existing.NumericValue = answer.NumericValue;
                existing.DateValue = answer.DateValue;
                existing.UpdatedAtUtc = DateTime.UtcNow;
                _context.ResponseAnswers.Update(existing);
            }
            else
            {
                answer.UpdatedAtUtc = DateTime.UtcNow;
                await _context.ResponseAnswers.AddAsync(answer);
            }
            await _context.SaveChangesAsync();
        }

        public async Task AddAnswerOptionAsync(ResponseAnswerOption answerOption)
        {
            await _context.Set<ResponseAnswerOption>().AddAsync(answerOption);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAnswerOptionsAsync(Guid responseId, Guid questionId)
        {
            var options = await GetAnswerOptionsAsync(responseId, questionId);
            _context.Set<ResponseAnswerOption>().RemoveRange(options);
            await _context.SaveChangesAsync();
        }
    }
}