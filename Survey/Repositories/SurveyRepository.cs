using Microsoft.EntityFrameworkCore;
using Survey.Models;
using SurveyModel = Survey.Models.Survey;

namespace Survey.Repositories
{
    public class SurveyRepository : ISurveyRepository
    {
        private readonly SurveyDbContext _context;

        public SurveyRepository(SurveyDbContext context)
        {
            _context = context;
        }

        public async Task<SurveyModel?> GetByIdAsync(Guid surveyId)
        {
            return await _context.Surveys
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.SurveyId == surveyId);
        }

        public async Task<SurveyModel?> GetByIdWithDetailsAsync(Guid surveyId)
        {
            return await _context.Surveys
                .Include(s => s.Owner)
                .Include(s => s.SurveyCollaborators)
                    .ThenInclude(sc => sc.User)
                .Include(s => s.Questions)
                .Include(s => s.SurveyResponses)
                .FirstOrDefaultAsync(s => s.SurveyId == surveyId);
        }

        public async Task<List<SurveyModel>> GetByOwnerIdAsync(Guid ownerId)
        {
            return await _context.Surveys
                .Include(s => s.Owner)
                .Where(s => s.OwnerId == ownerId)
                .OrderByDescending(s => s.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<List<SurveyModel>> GetAllAsync()
        {
            return await _context.Surveys
                .Include(s => s.Owner)
                .OrderByDescending(s => s.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task<bool> IsOwnerAsync(Guid surveyId, Guid userId)
        {
            return await _context.Surveys
                .AnyAsync(s => s.SurveyId == surveyId && s.OwnerId == userId);
        }

        public async Task AddAsync(SurveyModel survey)
        {
            await _context.Surveys.AddAsync(survey);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SurveyModel survey)
        {
            _context.Surveys.Update(survey);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid surveyId)
        {
            var survey = await GetByIdAsync(surveyId);
            if (survey != null)
            {
                _context.Surveys.Remove(survey);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetQuestionCountAsync(Guid surveyId)
        {
            return await _context.Questions
                .CountAsync(q => q.SurveyId == surveyId);
        }

        public async Task<int> GetResponseCountAsync(Guid surveyId)
        {
            return await _context.SurveyResponses
                .CountAsync(r => r.SurveyId == surveyId);
        }
    }
}