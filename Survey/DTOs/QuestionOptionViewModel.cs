using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    public class QuestionOptionViewModel
    {
        public Guid OptionId { get; set; }
        public Guid QuestionId { get; set; }

        [Required(ErrorMessage = "Option text is required")]
        [StringLength(500, ErrorMessage = "Option text cannot exceed 500 characters")]
        public string OptionText { get; set; } = string.Empty;

        public int OptionOrder { get; set; }
        public string? OptionValue { get; set; }
        public bool IsActive { get; set; } = true;
    }
}