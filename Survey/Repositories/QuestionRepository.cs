using Microsoft.EntityFrameworkCore;
using Survey.Models;

namespace Survey.Repositories
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly SurveyDbContext _context;

        public QuestionRepository(SurveyDbContext context)
        {
            _context = context;
        }

        public async Task<Question?> GetByIdAsync(Guid questionId)
        {
            return await _context.Questions
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);
        }

        public async Task<Question?> GetByIdWithOptionsAsync(Guid questionId)
        {
            return await _context.Questions
                .Include(q => q.QuestionOptions.OrderBy(o => o.OptionOrder))
                .FirstOrDefaultAsync(q => q.QuestionId == questionId);
        }

        public async Task<List<Question>> GetBySurveyIdAsync(Guid surveyId)
        {
            return await _context.Questions
                .Where(q => q.SurveyId == surveyId)
                .OrderBy(q => q.QuestionOrder)
                .ToListAsync();
        }

        public async Task<List<Question>> GetBySurveyIdWithOptionsAsync(Guid surveyId)
        {
            return await _context.Questions
                .Include(q => q.QuestionOptions.OrderBy(o => o.OptionOrder))
                .Where(q => q.SurveyId == surveyId)
                .OrderBy(q => q.QuestionOrder)
                .ToListAsync();
        }

        public async Task AddAsync(Question question)
        {
            await _context.Questions.AddAsync(question);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Question question)
        {
            _context.Questions.Update(question);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid questionId)
        {
            var question = await GetByIdAsync(questionId);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetMaxOrderBySurveyIdAsync(Guid surveyId)
        {
            var maxOrder = await _context.Questions
                .Where(q => q.SurveyId == surveyId)
                .MaxAsync(q => (int?)q.QuestionOrder);
            return maxOrder ?? 0;
        }
    }
}