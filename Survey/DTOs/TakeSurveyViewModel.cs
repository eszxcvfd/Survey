namespace Survey.DTOs
{
    /// <summary>
    /// View model for displaying current question to respondent
    /// </summary>
    public class TakeSurveyViewModel
    {
        public Guid ResponseId { get; set; }
        public Guid SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public string? SurveyDescription { get; set; }
        public QuestionViewModel CurrentQuestion { get; set; } = new();
        public int TotalQuestions { get; set; }
        public int CurrentQuestionNumber { get; set; }
        public bool IsLastQuestion { get; set; }
        public int ProgressPercentage { get; set; }
    }
}