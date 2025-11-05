using Microsoft.EntityFrameworkCore;
using Survey.Models;

namespace Survey.Repositories
{
    public class SurveyResponseRepository : ISurveyResponseRepository
    {
        private readonly SurveyDbContext _context;

        public SurveyResponseRepository(SurveyDbContext context)
        {
            _context = context;
        }

        public async Task<SurveyResponse?> GetByIdAsync(Guid responseId)
        {
            return await _context.SurveyResponses
                .Include(r => r.Survey)
                .Include(r => r.Channel)
                .FirstOrDefaultAsync(r => r.ResponseId == responseId);
        }

        public async Task<SurveyResponse?> GetByIdWithAnswersAsync(Guid responseId)
        {
            return await _context.SurveyResponses
                .Include(r => r.Survey)
                .Include(r => r.ResponseAnswers)
                    .ThenInclude(ra => ra.Question)
                .Include(r => r.ResponseAnswerOptions)
                    .ThenInclude(rao => rao.Option)
                .FirstOrDefaultAsync(r => r.ResponseId == responseId);
        }

        public async Task<List<SurveyResponse>> GetBySurveyIdAsync(Guid surveyId)
        {
            return await _context.SurveyResponses
                .Where(r => r.SurveyId == surveyId)
                .OrderByDescending(r => r.SubmittedAtUtc)
                .ToListAsync();
        }

        public async Task AddAsync(SurveyResponse response)
        {
            await _context.SurveyResponses.AddAsync(response);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SurveyResponse response)
        {
            _context.SurveyResponses.Update(response);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid responseId)
        {
            var response = await GetByIdAsync(responseId);
            if (response != null)
            {
                _context.SurveyResponses.Remove(response);
                await _context.SaveChangesAsync();
            }
        }
    }
}