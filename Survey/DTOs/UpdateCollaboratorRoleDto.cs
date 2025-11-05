using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    /// <summary>
    /// DTO for updating a collaborator's role
    /// </summary>
    public class UpdateCollaboratorRoleDto
    {
        [Required]
        public Guid SurveyId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string NewRole { get; set; } = string.Empty;
    }
}