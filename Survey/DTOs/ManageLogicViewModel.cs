namespace Survey.DTOs
{
    public class ManageLogicViewModel
    {
        public Guid SurveyId { get; set; }
        public string SurveyTitle { get; set; } = string.Empty;
        public List<BranchLogicRuleViewModel> Rules { get; set; } = new();
        public List<QuestionReferenceViewModel> AllQuestions { get; set; } = new();
        public AddRuleViewModel NewRuleForm { get; set; } = new();
    }
}