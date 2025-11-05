namespace Survey.DTOs
{
    /// <summary>
    /// View model for managing survey distribution channels
    /// </summary>
    public class ManageChannelsViewModel
    {
        public Guid SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public string SurveyStatus { get; set; } = string.Empty;
        public List<ChannelViewModel> ExistingChannels { get; set; } = new();
        public bool CanEdit { get; set; }
    }
}   