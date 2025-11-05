using Microsoft.EntityFrameworkCore;
using Survey.Models;

namespace Survey.Repositories
{
    public class SurveyChannelRepository : ISurveyChannelRepository
    {
        private readonly SurveyDbContext _context;

        public SurveyChannelRepository(SurveyDbContext context)
        {
            _context = context;
        }

        public async Task<SurveyChannel?> GetByIdAsync(Guid channelId)
        {
            return await _context.SurveyChannels
                .Include(c => c.Survey)
                .FirstOrDefaultAsync(c => c.ChannelId == channelId);
        }

        public async Task<SurveyChannel?> GetBySlugAsync(string slug)
        {
            return await _context.SurveyChannels
                .Include(c => c.Survey)
                    .ThenInclude(s => s.Questions.OrderBy(q => q.QuestionOrder))
                        .ThenInclude(q => q.QuestionOptions.OrderBy(o => o.OptionOrder))
                .FirstOrDefaultAsync(c => c.PublicUrlSlug == slug && c.IsActive);
        }

        public async Task<List<SurveyChannel>> GetBySurveyIdAsync(Guid surveyId)
        {
            return await _context.SurveyChannels
                .Where(c => c.SurveyId == surveyId)
                .OrderByDescending(c => c.CreatedAtUtc)
                .ToListAsync();
        }

        public async Task AddAsync(SurveyChannel channel)
        {
            await _context.SurveyChannels.AddAsync(channel);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(SurveyChannel channel)
        {
            _context.SurveyChannels.Update(channel);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid channelId)
        {
            var channel = await GetByIdAsync(channelId);
            if (channel != null)
            {
                _context.SurveyChannels.Remove(channel);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetResponseCountByChannelAsync(Guid channelId)
        {
            return await _context.SurveyResponses
                .CountAsync(r => r.ChannelId == channelId);
        }
    }
}