using Survey.Models;

namespace Survey.Repositories
{
    public interface ISurveyChannelRepository
    {
        Task<SurveyChannel?> GetByIdAsync(Guid channelId);
        Task<SurveyChannel?> GetBySlugAsync(string slug);
        Task<List<SurveyChannel>> GetBySurveyIdAsync(Guid surveyId);
        Task AddAsync(SurveyChannel channel);
        Task UpdateAsync(SurveyChannel channel);
        Task DeleteAsync(Guid channelId);
        Task<int> GetResponseCountByChannelAsync(Guid channelId);
    }
}