namespace Survey.DTOs
{
    public class OptionReferenceViewModel
    {
        public Guid OptionId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int OptionOrder { get; set; }
    }
}