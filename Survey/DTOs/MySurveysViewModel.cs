namespace Survey.DTOs
{
    /// <summary>
    /// View model for the My Surveys dashboard page
    /// </summary>
    public class MySurveysViewModel
    {
        public List<SurveyListItemViewModel> Surveys { get; set; } = new List<SurveyListItemViewModel>();
        public string CurrentFilter { get; set; } = "all";
        public int TotalSurveys { get; set; }
        public int OwnedSurveys { get; set; }
        public int SharedSurveys { get; set; }
        public int DraftSurveys { get; set; }
        public int PublishedSurveys { get; set; }
    }
}