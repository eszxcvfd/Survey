using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    public class AddRuleViewModel
    {
        public Guid SurveyId { get; set; }

        [Required(ErrorMessage = "Please select a source question")]
        public Guid SourceQuestionId { get; set; }

        [Required(ErrorMessage = "Please select an answer option")]
        public Guid SourceOptionId { get; set; }

        [Required(ErrorMessage = "Please select an action")]
        public string TargetAction { get; set; } = string.Empty;

        public Guid? TargetQuestionId { get; set; }
    }
}