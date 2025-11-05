namespace Survey.DTOs
{
    public class BranchLogicRuleViewModel
    {
        public Guid LogicId { get; set; }
        public Guid SourceQuestionId { get; set; }
        public string SourceQuestionText { get; set; } = string.Empty;
        public string ConditionDescription { get; set; } = string.Empty;
        public string TargetAction { get; set; } = string.Empty;
        public Guid? TargetQuestionId { get; set; }
        public string? TargetQuestionText { get; set; }
        public string RuleDescription { get; set; } = string.Empty;
        public int PriorityOrder { get; set; }
    }
}