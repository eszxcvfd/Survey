namespace Survey.DTOs
{
    public class SurveyDesignerViewModel
    {
        public Guid SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<QuestionViewModel> Questions { get; set; } = new();
    }
}