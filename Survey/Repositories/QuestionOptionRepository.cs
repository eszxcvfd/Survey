using Microsoft.EntityFrameworkCore;
using Survey.Models;

namespace Survey.Repositories
{
    public class QuestionOptionRepository : IQuestionOptionRepository
    {
        private readonly SurveyDbContext _context;

        public QuestionOptionRepository(SurveyDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionOption?> GetByIdAsync(Guid optionId)
        {
            return await _context.QuestionOptions
                .FirstOrDefaultAsync(o => o.OptionId == optionId);
        }

        public async Task<List<QuestionOption>> GetByQuestionIdAsync(Guid questionId)
        {
            return await _context.QuestionOptions
                .Where(o => o.QuestionId == questionId)
                .OrderBy(o => o.OptionOrder)
                .ToListAsync();
        }

        public async Task AddAsync(QuestionOption option)
        {
            await _context.QuestionOptions.AddAsync(option);
            await _context.SaveChangesAsync();
        }

        public async Task AddRangeAsync(List<QuestionOption> options)
        {
            await _context.QuestionOptions.AddRangeAsync(options);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(QuestionOption option)
        {
            _context.QuestionOptions.Update(option);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid optionId)
        {
            var option = await GetByIdAsync(optionId);
            if (option != null)
            {
                _context.QuestionOptions.Remove(option);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteRangeAsync(List<Guid> optionIds)
        {
            var options = await _context.QuestionOptions
                .Where(o => optionIds.Contains(o.OptionId))
                .ToListAsync();
            
            if (options.Any())
            {
                _context.QuestionOptions.RemoveRange(options);
                await _context.SaveChangesAsync();
            }
        }
    }
}