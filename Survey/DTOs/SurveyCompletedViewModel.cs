namespace Survey.DTOs
{
    /// <summary>
    /// View model for thank you page after survey completion
    /// </summary>
    public class SurveyCompletedViewModel
    {
        public Guid ResponseId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public string ThankYouMessage { get; set; } = "Thank you for completing this survey!";
        public DateTime CompletedAt { get; set; }
    }
}