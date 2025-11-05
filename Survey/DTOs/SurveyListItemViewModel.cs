namespace Survey.DTOs
{
    /// <summary>
    /// View model for displaying a survey in a list
    /// </summary>
    public class SurveyListItemViewModel
    {
        public Guid SurveyId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public string MyRole { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public int ResponseCount { get; set; }
        public int QuestionCount { get; set; }
        public DateTime? OpenAtUtc { get; set; }
        public DateTime? CloseAtUtc { get; set; }
    }
}