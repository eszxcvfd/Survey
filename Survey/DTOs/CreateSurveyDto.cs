using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    /// <summary>
    /// DTO for creating a new survey
    /// </summary>
    public class CreateSurveyDto
    {
        [Required(ErrorMessage = "Survey title is required")]
        [StringLength(255, ErrorMessage = "Title cannot exceed 255 characters")]
        public string Title { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        public bool IsAnonymous { get; set; } = false;

        [StringLength(20)]
        public string? DefaultLanguage { get; set; } = "en";
    }
}