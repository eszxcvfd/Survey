using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    public class QuestionViewModel
    {
        public Guid QuestionId { get; set; }
        public Guid SurveyId { get; set; }

        [Required(ErrorMessage = "Question text is required")]
        [StringLength(2000, ErrorMessage = "Question text cannot exceed 2000 characters")]
        public string QuestionText { get; set; } = string.Empty;

        [Required(ErrorMessage = "Question type is required")]
        public string QuestionType { get; set; } = string.Empty;

        public int QuestionOrder { get; set; }
        public bool IsRequired { get; set; }
        
        public string? ValidationRule { get; set; }
        public string? HelpText { get; set; }
        public string? DefaultValue { get; set; }

        public List<QuestionOptionViewModel> Options { get; set; } = new();
    }
}