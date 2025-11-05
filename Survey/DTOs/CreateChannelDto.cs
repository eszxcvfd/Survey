using System.ComponentModel.DataAnnotations;

namespace Survey.DTOs
{
    /// <summary>
    /// DTO for creating a new distribution channel
    /// </summary>
    public class CreateChannelDto
    {
        [Required]
        public Guid SurveyId { get; set; }

        [Required(ErrorMessage = "Channel type is required")]
        public string ChannelType { get; set; } = string.Empty; // "Link", "Email"

        // *** MỚI: Cho phép nhập nhiều email ***
        public string? RecipientEmails { get; set; } // Danh sách email cách nhau bởi dấu phẩy hoặc xuống dòng

        [StringLength(255)]
        public string? EmailSubject { get; set; }

        public string? EmailBody { get; set; }
    }
}