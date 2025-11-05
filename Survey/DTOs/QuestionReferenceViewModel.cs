namespace Survey.DTOs
{
    public class QuestionReferenceViewModel
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int QuestionOrder { get; set; }
        public string QuestionType { get; set; } = string.Empty;
        public List<OptionReferenceViewModel> Options { get; set; } = new();
    }
}