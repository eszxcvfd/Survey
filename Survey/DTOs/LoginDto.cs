using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        // New properties for CAPTCHA support
        public bool ShowCaptcha { get; set; }
        public string? CaptchaResponse { get; set; }
    }
}