namespace Survey.DTOs
{
    /// <summary>
    /// View model for displaying a single channel
    /// </summary>
    public class ChannelViewModel
    {
        public Guid ChannelId { get; set; }
        public string ChannelType { get; set; } = string.Empty; // "Link", "QR", "Email"
        public string? PublicUrlSlug { get; set; }
        public string? FullUrl { get; set; }
        public string? QrImagePath { get; set; }
        public string? EmailSubject { get; set; }
        public DateTime? SentAtUtc { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int ResponseCount { get; set; }
    }
}