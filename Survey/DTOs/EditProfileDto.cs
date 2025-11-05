using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    public class EditProfileDto
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(255, ErrorMessage = "Full name cannot exceed 255 characters")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        public string? CurrentAvatarUrl { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? AvatarImage { get; set; }
    }
}