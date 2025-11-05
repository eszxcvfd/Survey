using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    /// <summary>
    /// DTO for submitting answer to a question
    /// </summary>
    public class SubmitAnswerViewModel
    {
        [Required]
        public Guid ResponseId { get; set; }

        [Required]
        public Guid QuestionId { get; set; }

        // For text-based questions
        public string? AnswerText { get; set; }

        // For numeric questions
        public decimal? NumericValue { get; set; }

        // For date questions
        public DateTime? DateValue { get; set; }

        // For single-choice questions (MultipleChoice, Dropdown)
        public Guid? SelectedOptionId { get; set; }

        // For multi-choice questions (Checkboxes)
        public List<Guid> SelectedOptionIds { get; set; } = new();

        // For "Other" option with additional text
        public string? AdditionalText { get; set; }
    }
}