using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    public class SurveySettingsViewModel
    {
        public Guid SurveyId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [StringLength(20, ErrorMessage = "Language code cannot exceed 20 characters")]
        [Display(Name = "Default Language")]
        public string? DefaultLanguage { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Survey Status")]
        public string Status { get; set; } = "Draft";

        [Display(Name = "Anonymous Responses")]
        public bool IsAnonymous { get; set; }

        [Display(Name = "Opening Date (UTC)")]
        [DataType(DataType.DateTime)]
        public DateTime? OpenAtUtc { get; set; }

        [Display(Name = "Closing Date (UTC)")]
        [DataType(DataType.DateTime)]
        public DateTime? CloseAtUtc { get; set; }

        [Display(Name = "Response Quota")]
        [Range(1, int.MaxValue, ErrorMessage = "Response quota must be at least 1")]
        public int? ResponseQuota { get; set; }

        [StringLength(50, ErrorMessage = "Quota behavior cannot exceed 50 characters")]
        [Display(Name = "Quota Behavior")]
        public string? QuotaBehavior { get; set; }

        // Read-only properties for display
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public int QuestionCount { get; set; }
        public int ResponseCount { get; set; }
    }
}