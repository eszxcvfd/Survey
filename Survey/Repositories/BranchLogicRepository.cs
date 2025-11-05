using Microsoft.EntityFrameworkCore;
using Survey.Models;

namespace Survey.Repositories
{
    public class BranchLogicRepository : IBranchLogicRepository
    {
        private readonly SurveyDbContext _context;

        public BranchLogicRepository(SurveyDbContext context)
        {
            _context = context;
        }

        public async Task<BranchLogic?> GetByIdAsync(Guid logicId)
        {
            return await _context.BranchLogics
                .Include(bl => bl.SourceQuestion)
                    .ThenInclude(q => q.QuestionOptions)
                .Include(bl => bl.TargetQuestion)
                .FirstOrDefaultAsync(bl => bl.LogicId == logicId);
        }

        public async Task<List<BranchLogic>> GetBySurveyIdAsync(Guid surveyId)
        {
            return await _context.BranchLogics
                .Include(bl => bl.SourceQuestion)
                    .ThenInclude(q => q.QuestionOptions)
                .Include(bl => bl.TargetQuestion)
                .Where(bl => bl.SurveyId == surveyId)
                .OrderBy(bl => bl.PriorityOrder)
                .ToListAsync();
        }

        public async Task<List<BranchLogic>> GetBySourceQuestionIdAsync(Guid sourceQuestionId)
        {
            return await _context.BranchLogics
                .Include(bl => bl.SourceQuestion)
                .Include(bl => bl.TargetQuestion)
                .Where(bl => bl.SourceQuestionId == sourceQuestionId)
                .OrderBy(bl => bl.PriorityOrder)
                .ToListAsync();
        }

        public async Task AddAsync(BranchLogic rule)
        {
            await _context.BranchLogics.AddAsync(rule);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BranchLogic rule)
        {
            _context.BranchLogics.Update(rule);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid logicId)
        {
            var rule = await GetByIdAsync(logicId);
            if (rule != null)
            {
                _context.BranchLogics.Remove(rule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetMaxPriorityOrderBySurveyIdAsync(Guid surveyId)
        {
            var maxOrder = await _context.BranchLogics
                .Where(bl => bl.SurveyId == surveyId)
                .MaxAsync(bl => (int?)bl.PriorityOrder);
            return maxOrder ?? 0;
        }
    }
}