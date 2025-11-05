namespace Survey.DTOs
{
    public class QuestionLogicRuleViewModel
    {
        public Guid LogicId { get; set; }
        public Guid SourceOptionId { get; set; }
        public string SourceOptionText { get; set; } = string.Empty;
        public string TargetAction { get; set; } = string.Empty;
        public string ActionDescription { get; set; } = string.Empty;
    }
}