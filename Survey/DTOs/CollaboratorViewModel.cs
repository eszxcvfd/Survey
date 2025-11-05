namespace Survey.DTOs
{
    /// <summary>
    /// View model for displaying a single collaborator in the list
    /// </summary>
    public class CollaboratorViewModel
    {
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime GrantedAtUtc { get; set; }
        public Guid? GrantedBy { get; set; }
        public string? GrantedByName { get; set; }
    }
}