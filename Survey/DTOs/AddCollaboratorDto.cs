using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    /// <summary>
    /// DTO for adding a new collaborator to a survey
    /// </summary>
    public class AddCollaboratorDto
    {
        [Required]
        public Guid SurveyId { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; } = string.Empty;
    }
}