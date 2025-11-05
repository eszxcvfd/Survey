using Survey.DTOs;

namespace Survey.Services
{
    public interface ISurveyChannelService
    {
        Task<ManageChannelsViewModel?> GetChannelsForSurveyAsync(Guid surveyId, Guid currentUserId);
        Task<ServiceResult<ChannelViewModel>> CreateChannelAsync(CreateChannelDto model, Guid currentUserId, string baseUrl);
        Task<ServiceResult> DeleteChannelAsync(Guid channelId, Guid currentUserId);
        Task<ServiceResult> ToggleChannelStatusAsync(Guid channelId, Guid currentUserId);
    }
}